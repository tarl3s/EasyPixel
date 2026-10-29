using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Tools;
using PixelRect = EasyPixel.Core.Model.PixelRect;

namespace EasyPixel.UiTests;

public class ResizeHandleTests
{
    private static readonly uint Red = new Rgba(255, 0, 0, 255).Packed;

    // a 2x2 red square at (2,2), selected with the rectangle selection tool
    private static Harness Setup()
    {
        var h = new Harness(16, 16);
        var img = h.Doc.GetOrCreateCel(h.Session.Layer, 0).Image;
        for (int y = 2; y < 4; y++) for (int x = 2; x < 4; x++) img.Set(x, y, Red);
        h.Session.InvalidateAll();
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(2, 2, 2, 2));
        Harness.Pump();
        return h;
    }

    // window point of a canvas corner (between pixels), where the handles sit
    private static Point Corner(Harness h, int x, int y) =>
        h.Window.Canvas.TranslatePoint(h.Window.Canvas.SpriteToScreen(x, y), h.Window)!.Value;

    private static void DragFrom(Harness h, Point from, (int X, int Y) toPixel, RawInputModifiers mods = RawInputModifiers.None)
    {
        h.Window.MouseDown(from, MouseButton.Left, mods);
        h.Window.MouseMove(h.At(toPixel.X, toPixel.Y), mods);
        h.Window.MouseUp(h.At(toPixel.X, toPixel.Y), MouseButton.Left, mods);
        Harness.Pump();
    }

    [AvaloniaFact]
    public void DraggingTheCornerHandle_StretchesTheSelectedPixels_OneUndoStep()
    {
        using var h = Setup();
        DragFrom(h, Corner(h, 4, 4), (7, 7)); // bottom-right handle to pixel (7,7): box 2..7 = 6x6
        Assert.NotNull(h.Session.Float);
        Assert.Equal(new PixelRect(2, 2, 6, 6), h.Session.Float!.Bounds);
        Assert.Equal(ToolKind.RectSelect, h.Window.Tools.Current);
        h.Key(Key.Enter);
        Assert.Equal(Red, h.Pixel(2, 2));
        Assert.Equal(Red, h.Pixel(7, 7));
        Assert.Equal(0u, h.Pixel(8, 8));

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(Red, h.Pixel(3, 3));
        Assert.Equal(0u, h.Pixel(5, 5));
    }

    [AvaloniaFact]
    public void SideHandle_ChangesOneAxis_AndShiftKeepsProportions()
    {
        using var h = Setup();
        DragFrom(h, Corner(h, 4, 3), (5, 3)); // right side handle (middle of the right edge)
        Assert.Equal(new PixelRect(2, 2, 4, 2), h.Session.Float!.Bounds);

        h.Key(Key.Escape); // throws the float away
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(2, 2, 2, 2));
        Harness.Pump();
        DragFrom(h, Corner(h, 4, 4), (9, 5), RawInputModifiers.Shift);
        var b = h.Session.Float!.Bounds;
        Assert.Equal(b.W, b.H); // still square
        Assert.Equal(8, b.W);
    }

    [AvaloniaFact]
    public void NoHandles_WithAPaintTool()
    {
        using var h = Setup();
        h.Window.Tools.Current = ToolKind.Pencil;
        Assert.Null(h.Window.Canvas.HandleAt(h.Window.Canvas.SpriteToScreen(4, 4)));
        h.Window.Tools.Current = ToolKind.Move;
        Assert.NotNull(h.Window.Canvas.HandleAt(h.Window.Canvas.SpriteToScreen(4, 4)));
    }
}
