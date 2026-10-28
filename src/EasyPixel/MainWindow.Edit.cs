using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using EasyPixel.Core.Formats;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Tools;

namespace EasyPixel;

// Edit menu: undo/redo, cut/copy/paste (system clipboard as PNG, with an in-app copy as fallback), delete, select all/none.
public partial class MainWindow
{
    // "PNG" is the clipboard format other image apps on Windows read and write.
    private static readonly DataFormat<byte[]> PngFormat = DataFormat.CreateBytesPlatformFormat("PNG");

    /// <summary>The last copied picture (RGBA), used when the system clipboard has no picture for us.</summary>
    internal static Image? InternalClipboard { get; private set; }

    private void Undo_Click(object? sender, RoutedEventArgs e) => Undo();
    private void Redo_Click(object? sender, RoutedEventArgs e) => Redo();
    private void Cut_Click(object? sender, RoutedEventArgs e) => _ = CutAsync();
    private void Copy_Click(object? sender, RoutedEventArgs e) => _ = CopyAsync();
    private void Paste_Click(object? sender, RoutedEventArgs e) => _ = PasteAsync();
    private void Delete_Click(object? sender, RoutedEventArgs e) => DeleteSelection();
    private void SelectAll_Click(object? sender, RoutedEventArgs e) => SelectAll();
    private void Deselect_Click(object? sender, RoutedEventArgs e) => Deselect();

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
        if (Session.WhyNotEditable() is { } why) { ShowStatus(why); return; }
        Session.DropFloating();
        var doc = Session.Doc;
        var cel = Session.CelForDrawing();
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
        Session.BeginFloating(new Floating(pixels, x, y, clear), cel, before, "Paste", selectionBefore);
        Tools.Current = ToolKind.Move;
        UpdateTitle();
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
            for (int i = 0; i < src.Pixels.Length; i++) copy.Pixels[i] = src.Pixels[i] == fs.Pixels.Clear ? 0 : ToRgba(doc, src.Pixels[i]);
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
                img.Pixels[(y - b.Y) * b.W + (x - b.X)] = ToRgba(doc, cel.Image.Pixels[cy * cel.Image.Width + cx]);
            }
        return img;
    }

    private static uint ToRgba(Sprite doc, uint v)
    {
        if (doc.Mode == ColorMode.Rgba) return v;
        var pal = doc.Palette;
        return (int)v == pal.TransparentIndex || v >= (uint)pal.Colors.Count ? 0 : pal.Colors[(int)v].Packed;
    }
}
