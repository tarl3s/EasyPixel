using Avalonia.Headless.XUnit;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Tools;

namespace EasyPixel.UiTests;

public class JumbleBlurTests
{
    private static readonly uint R = new Rgba(255, 0, 0, 255).Packed, B = new Rgba(0, 0, 255, 255).Packed;

    private static Harness Halves()
    {
        var h = new Harness(16, 16);
        var img = h.Doc.GetOrCreateCel(h.Session.Layer, 0).Image;
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) img.Set(x, y, x < 8 ? R : B);
        h.Session.InvalidateAll();
        return h;
    }

    [AvaloniaFact]
    public void Jumble_Dragged_MixesTheEdge_KeepsTheColours_OneUndo()
    {
        using var h = Halves();
        h.Window.Tools.Current = ToolKind.Jumble;
        h.Window.Tools.SetBrush(4, BrushShape.Square);
        h.Window.Tools.JumbleAmount = 100;
        h.Drag((8, 2), (8, 6), (8, 10), (8, 13), (7, 8));
        var px = h.Doc.Cels[(h.Session.Layer, 0)].Image.Pixels;
        Assert.Equal(128, px.Count(p => p == R));
        Assert.Equal(128, px.Count(p => p == B));
        Assert.Contains(Enumerable.Range(0, 16), y => h.Pixel(8, y) == R || h.Pixel(7, y) == B);
        h.Key(Avalonia.Input.Key.Z, Avalonia.Input.RawInputModifiers.Control);
        for (int y = 0; y < 16; y++) { Assert.Equal(R, h.Pixel(7, y)); Assert.Equal(B, h.Pixel(8, y)); }
    }

    [AvaloniaFact]
    public void Blur_Dragged_SoftensTheEdge()
    {
        using var h = Halves();
        h.Window.Tools.Current = ToolKind.Blur;
        h.Window.Tools.SetBrush(3, BrushShape.Square);
        h.Window.Tools.BlurAmount = 100;
        h.Drag((8, 4), (8, 8));
        var c = Rgba.FromPacked(h.Pixel(8, 6));
        Assert.True(c.R > 0 && c.B > 0);
        Assert.Equal(R, h.Pixel(2, 6)); // away from the brush: untouched
    }
}
