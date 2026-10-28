using Avalonia.Interactivity;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Tools;

namespace EasyPixel;

// Sprite menu: canvas size, resize, rotate, flip, colour mode (whole sprite) and outline / pixelate (current cel),
// reduce to palette. Each is one undo step.
public partial class MainWindow
{
    private void CanvasSize_Click(object? sender, RoutedEventArgs e) => _ = CanvasSizeAsync();
    private void Resize_Click(object? sender, RoutedEventArgs e) => _ = ResizeAsync();
    private void RotateCw_Click(object? sender, RoutedEventArgs e) => ChangeSprite("Rotate", s => SpriteTransforms.Rotate(s, SpriteRotation.Clockwise90));
    private void RotateCcw_Click(object? sender, RoutedEventArgs e) => ChangeSprite("Rotate", s => SpriteTransforms.Rotate(s, SpriteRotation.CounterClockwise90));
    private void Rotate180_Click(object? sender, RoutedEventArgs e) => ChangeSprite("Rotate", s => SpriteTransforms.Rotate(s, SpriteRotation.Half));
    private void FlipH_Click(object? sender, RoutedEventArgs e) => ChangeSprite("Flip", s => SpriteTransforms.Flip(s, horizontal: true));
    private void FlipV_Click(object? sender, RoutedEventArgs e) => ChangeSprite("Flip", s => SpriteTransforms.Flip(s, horizontal: false));
    private void ModeRgba_Click(object? sender, RoutedEventArgs e) => SetColorMode(ColorMode.Rgba);
    private void ModeIndexed_Click(object? sender, RoutedEventArgs e) => SetColorMode(ColorMode.Indexed);
    private void Outline_Click(object? sender, RoutedEventArgs e) => OutlineCel();
    private void Pixelate_Click(object? sender, RoutedEventArgs e) => _ = PixelateAsync();
    private void ReduceColors_Click(object? sender, RoutedEventArgs e) => _ = ReduceColorsAsync();

    /// <summary>Runs a change on the whole sprite as one undo step.</summary>
    internal void ChangeSprite(string editName, Action<Sprite> change)
    {
        Session.DropFloating();
        Canvas.FinishGesture();
        var doc = Session.Doc;
        int w = doc.Width, h = doc.Height;
        try
        {
            Session.Apply(SpriteStateEdit.Perform(doc, editName, () => change(doc)));
        }
        catch (Exception ex) when (ex is OutOfMemoryException or OverflowException)
        {
            _ = Dialogs.Tell(L.T("There isn't enough memory for a canvas that big."));
            return;
        }
        if (doc.Width != w || doc.Height != h) Canvas.FitToView();
        UpdateStatusBar();
    }

    private async Task<bool> ConfirmLarge(int w, int h) =>
        (w <= LargeCanvas && h <= LargeCanvas)
        || await Dialogs.Confirm(L.F("A {0} x {1} canvas is very large: it may be slow or run out of memory. Create it anyway?", w, h), L.T("Create"), L.T("Cancel"));

    /// <summary>New canvas size; the picture stays where the anchor says (nothing is scaled or cut).</summary>
    internal async Task CanvasSizeAsync()
    {
        var doc = Session.Doc;
        if (await Dialogs.AskCanvasSize(doc.Width, doc.Height) is not { } o || o.Width < 1 || o.Height < 1) return;
        if ((o.Width, o.Height) == (doc.Width, doc.Height) || !await ConfirmLarge(o.Width, o.Height)) return;
        Session.DropFloating();
        Session.Apply(ResizeEdit.Perform(doc, o.Width, o.Height, o.Anchor));
        Canvas.FitToView();
        UpdateStatusBar();
    }

    /// <summary>Scales the whole sprite (nearest neighbour).</summary>
    internal async Task ResizeAsync()
    {
        var doc = Session.Doc;
        if (await Dialogs.AskSize(L.T("Resize sprite"), doc.Width, doc.Height) is not { } size || size.Width < 1 || size.Height < 1) return;
        if ((size.Width, size.Height) == (doc.Width, doc.Height) || !await ConfirmLarge(size.Width, size.Height)) return;
        ChangeSprite("Resize sprite", s => SpriteTransforms.Scale(s, size.Width, size.Height));
    }

    internal void SetColorMode(ColorMode mode)
    {
        if (Session.Doc.Mode == mode) return;
        ChangeSprite("Colour mode", s => s.ConvertMode(mode));
        UpdateModeMenu();
    }

    private void UpdateModeMenu()
    {
        ModeRgbaItem.IsChecked = Session.Doc.Mode == ColorMode.Rgba;
        ModeIndexedItem.IsChecked = Session.Doc.Mode == ColorMode.Indexed;
    }

    /// <summary>Draws a one-pixel outline in the primary colour around what is drawn on the current cel (inside the selection, if any).</summary>
    internal void OutlineCel()
    {
        if (Canvas.Context is not { } ctx || Stroke.Begin(ctx, "Outline") is not { } st) return;
        var doc = Session.Doc;
        uint clear = doc.TransparentPixel;
        var sink = st.Step();
        Outline.Apply(sink, doc.ColorToPixel(Tools.Primary), outside: true, diagonal: false,
            doc.Mode == ColorMode.Indexed ? v => v == clear : null);
        st.EndStep(sink);
        st.Commit();
        Session.InvalidateAll();
    }

    internal async Task PixelateAsync()
    {
        if (await Dialogs.AskNumber(L.T("Pixelate"), L.T("Block size (pixels)"), 2, 2, 256) is not { } cell) return;
        if (Canvas.Context is not { } ctx || Stroke.Begin(ctx, "Pixelate") is not { } st) return;
        var sink = st.Step();
        Pixelate.Apply(sink, cell);
        st.EndStep(sink);
        st.Commit();
    }

    /// <summary>Keeps at most N colours (median cut over every cel).</summary>
    internal async Task ReduceColorsAsync()
    {
        if (await Dialogs.AskNumber(L.T("Reduce colours"), L.T("Colours to keep"), 16, 2, 256) is not { } n) return;
        ChangeSprite("Reduce colours", s => SpriteTransforms.ReduceColors(s, Session.Doc.Mode == ColorMode.Indexed ? Math.Min(n, 255) : n));
    }
}
