using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using EasyPixel.Core.Formats;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Tools;

namespace EasyPixel;

// Edit menu: undo/redo, cut/copy/paste (system clipboard as PNG, with an in-app copy as fallback), delete, flip / rotate /
// scale the selection (or the layer), select all/none.
public partial class MainWindow
{
    // "PNG" is the clipboard format other image apps on Windows read and write.
    private static readonly DataFormat<byte[]> PngFormat = DataFormat.CreateBytesPlatformFormat("PNG");

    /// <summary>The last copied picture (RGBA), used when the system clipboard has no picture for us.</summary>
    internal static Image? InternalClipboard { get; set; }

    private void Undo_Click(object? sender, RoutedEventArgs e) => Undo();
    private void Redo_Click(object? sender, RoutedEventArgs e) => Redo();
    private void Cut_Click(object? sender, RoutedEventArgs e) => _ = CutAsync();
    private void Copy_Click(object? sender, RoutedEventArgs e) => _ = CopyAsync();
    private void Paste_Click(object? sender, RoutedEventArgs e) => _ = PasteAsync();
    private void Delete_Click(object? sender, RoutedEventArgs e) => DeleteSelection();
    private void SelectAll_Click(object? sender, RoutedEventArgs e) => SelectAll();
    private void Deselect_Click(object? sender, RoutedEventArgs e) => Deselect();
    private void FlipSelH_Click(object? sender, RoutedEventArgs e) => FlipSelection(horizontal: true);
    private void FlipSelV_Click(object? sender, RoutedEventArgs e) => FlipSelection(horizontal: false);
    private void RotateSelCw_Click(object? sender, RoutedEventArgs e) => RotateSelection(clockwise: true);
    private void RotateSelCcw_Click(object? sender, RoutedEventArgs e) => RotateSelection(clockwise: false);
    private void ScaleSel_Click(object? sender, RoutedEventArgs e) => _ = ScaleSelectionAsync();

    internal void Undo()
    {
        Canvas.CancelGesture();
        if (!Session.Undo()) ShowStatus(L.T("Nothing to undo"));
    }

    internal void Redo()
    {
        Canvas.CancelGesture();
        if (!Session.Redo()) ShowStatus(L.T("Nothing to redo"));
    }

    internal void SelectAll()
    {
        Session.DropFloating();
        Session.Sel = Selection.All(Session.Doc.Width, Session.Doc.Height);
    }

    internal void Deselect()
    {
        Session.DropFloating();
        Session.Sel = Selection.None(Session.Doc.Width, Session.Doc.Height);
    }

    /// <summary>Clears the selected pixels of the current layer (or throws away floating pixels).</summary>
    internal void DeleteSelection()
    {
        if (Session.Float != null) { Session.DeleteFloating(); return; }
        if (Session.Sel.IsEmpty) { ShowStatus(L.T("Select something first")); return; }
        if (Canvas.Context is not { } ctx || Stroke.Begin(ctx, "Delete") is not { } st) return;
        var b = Session.Sel.Bounds;
        var sink = st.Step();
        uint clear = Session.Doc.TransparentPixel;
        for (int y = b.Y; y < b.Bottom; y++) sink.SetSpan(st.ToImageY(y), st.ToImageX(b.X), st.ToImageX(b.Right - 1), clear);
        st.EndStep(sink);
        st.Commit();
    }

    // ---- transforms (Aseprite's Edit → Flip / Rotate / Scale) ----

    internal void FlipSelection(bool horizontal) =>
        TransformSelection(L.N("Flip"), f => { if (horizontal) f.FlipH(); else f.FlipV(); });

    internal void RotateSelection(bool clockwise) => TransformSelection(L.N("Rotate"), f => f.Rotate90(clockwise));

    internal async Task ScaleSelectionAsync()
    {
        if (await Dialogs.AskNumber(L.T("Scale"), L.T("Size (%)"), 100, 1, 1000) is not { } percent || percent == 100) return;
        TransformSelection(L.N("Scale"), f => f.Scale(
            Math.Max(1, (int)Math.Round(f.Pixels.Width * percent / 100.0)), Math.Max(1, (int)Math.Round(f.Pixels.Height * percent / 100.0))));
    }

    /// <summary>
    /// Changes the floating pixels, lifting the selection first (they stay floating: move them, then Enter drops them).
    /// With nothing selected the whole layer changes at once, as one undo step.
    /// </summary>
    private void TransformSelection(string editName, Action<Floating> change)
    {
        Canvas.FinishGesture();
        bool wholeLayer = false;
        if (Session.Float == null)
        {
            if (Session.WhyNotEditable() is { } why) { ShowStatus(why); return; }
            wholeLayer = Session.Sel.IsEmpty;
            if (wholeLayer) Session.Sel = Selection.All(Session.Doc.Width, Session.Doc.Height);
            Session.LiftSelection(editName);
        }
        Session.TransformFloating(change);
        if (wholeLayer)
        {
            Session.DropFloating();
            Session.Sel = Selection.None(Session.Doc.Width, Session.Doc.Height);
        }
        UpdateTitle();
    }

    internal async Task CopyAsync()
    {
        var img = SelectedPicture();
        if (img == null) { ShowStatus(L.T("Nothing to copy")); return; }
        InternalClipboard = img;
        if (Clipboard is not { } cb) return;
        try
        {
            await cb.SetValueAsync(PngFormat, Png.Encode(img));
        }
        catch (Exception) { /* no system clipboard: the in-app copy still works */ }
    }

    internal async Task CutAsync()
    {
        // Like Delete: nothing selected means nothing to cut (copy alone would copy the whole layer).
        if (Session.Float == null && Session.Sel.IsEmpty) { ShowStatus(L.T("Select something first")); return; }
        await CopyAsync();
        DeleteSelection();
    }

    /// <summary>Pastes as floating pixels (moved with the Move tool, dropped by Enter or another tool).</summary>
    internal async Task PasteAsync()
    {
        var img = await SystemClipboardPicture() ?? InternalClipboard;
        if (img == null) { ShowStatus(L.T("The clipboard has no picture")); return; }
        PlaceFloating(img, "Paste");
    }

    /// <summary>
    /// Puts an RGBA picture on the current layer as floating pixels (centred, or at the selection), ready to be moved.
    /// False when the layer can't be edited (the reason is shown).
    /// </summary>
    internal bool PlaceFloating(Image img, string editName)
    {
        if (Session.WhyNotEditable() is { } why) { ShowStatus(why); return false; }
        Session.DropFloating();
        var doc = Session.Doc;
        // Colours the picture adds to an indexed palette become part of the paste's undo step.
        var paletteBefore = doc.Mode == ColorMode.Indexed ? PaletteSnapshot.Take(doc) : null;
        doc.EnsureTransparentIndex();
        var cel = Session.CelForDrawing(out var added);
        var before = CelReplaceEdit.Begin(cel);
        uint clear = doc.TransparentPixel;
        var pixels = new Image(img.Width, img.Height, clear);
        for (int i = 0; i < img.Pixels.Length; i++)
        {
            var c = Rgba.FromPacked(img.Pixels[i]);
            pixels.Pixels[i] = c.A == 0 ? clear : doc.ColorToPixel(c);
        }
        int x = Session.Sel.IsEmpty ? (doc.Width - img.Width) / 2 : Session.Sel.Bounds.X;
        int y = Session.Sel.IsEmpty ? (doc.Height - img.Height) / 2 : Session.Sel.Bounds.Y;
        var selectionBefore = Session.Sel;
        Session.Sel = Selectors.Rect(doc.Width, doc.Height, new PixelRect(x, y, img.Width, img.Height));
        Session.BeginFloating(new Floating(pixels, x, y, clear), cel, before, editName, selectionBefore, paletteBefore, added);
        Tools.Current = ToolKind.Move;
        UpdateTitle();
        return true;
    }

    private async Task<Image?> SystemClipboardPicture()
    {
        if (Clipboard is not { } cb) return null;
        try
        {
            return await cb.TryGetValueAsync(PngFormat) is { Length: > 0 } bytes ? Png.Decode(bytes) : null;
        }
        catch (Exception) { return null; } // unreadable or foreign data: fall back to the in-app copy
    }

    /// <summary>The selected pixels of the current layer as RGBA (all of the canvas when nothing is selected), or the floating pixels.</summary>
    private Image? SelectedPicture()
    {
        var doc = Session.Doc;
        if (Session.Float is { } fs)
        {
            var src = fs.Pixels.Pixels;
            var copy = new Image(src.Width, src.Height);
            for (int i = 0; i < src.Pixels.Length; i++) copy.Pixels[i] = src.Pixels[i] == fs.Pixels.Clear ? 0 : doc.ToPacked(src.Pixels[i]);
            return copy;
        }
        if (!doc.Cels.TryGetValue((Session.Layer, Session.Frame), out var cel)) return null;
        var sel = Session.Sel;
        var b = sel.IsEmpty ? Session.CanvasRect : sel.Bounds;
        var img = new Image(b.W, b.H);
        for (int y = b.Y; y < b.Bottom; y++)
            for (int x = b.X; x < b.Right; x++)
            {
                if (!sel.IsEmpty && !sel.Contains(x, y)) continue;
                int cx = x - cel.X, cy = y - cel.Y;
                if ((uint)cx >= (uint)cel.Image.Width || (uint)cy >= (uint)cel.Image.Height) continue;
                img.Pixels[(y - b.Y) * b.W + (x - b.X)] = doc.ToPacked(cel.Image.Pixels[cy * cel.Image.Width + cx]);
            }
        return img;
    }
}
