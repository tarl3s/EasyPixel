using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Tools;
using PixelRect = EasyPixel.Core.Model.PixelRect;

namespace EasyPixel.UiTests;

public class RotateHandleTests
{
    private static readonly uint Red = new Rgba(255, 0, 0, 255).Packed;

    // a 6x2 red bar at (5,7), selected with the rectangle selection tool
    private static Harness Setup()
    {
        var h = new Harness(16, 16);
        var img = h.Doc.GetOrCreateCel(h.Session.Layer, 0).Image;
        for (int y = 7; y < 9; y++) for (int x = 5; x < 11; x++) img.Set(x, y, Red);
        h.Session.InvalidateAll();
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(5, 7, 6, 2));
        Harness.Pump();
        return h;
    }

    // a window point just outside the bottom-right corner of the selection (canvas px 11,9)
    private static Point OutsideCorner(Harness h) =>
        h.Window.Canvas.TranslatePoint(h.Window.Canvas.SpriteToScreen(11, 9) + new Vector(12, 12), h.Window)!.Value;

    [AvaloniaFact]
    public void JustOutsideACorner_IsTheTurnZone_NotInsideOrFarAway()
    {
        using var h = Setup();
        var c = h.Window.Canvas;
        Assert.True(c.RotateZoneAt(c.SpriteToScreen(11, 9) + new Vector(12, 12)));
        Assert.False(c.RotateZoneAt(c.SpriteToScreen(8, 8)));                    // inside: moving
        Assert.False(c.RotateZoneAt(c.SpriteToScreen(11, 9) + new Vector(80, 80))); // far away
        Assert.False(c.RotateZoneAt(c.SpriteToScreen(11, 9)));                    // on the handle: resizing
        h.Window.Tools.Current = ToolKind.Pencil;
        Assert.False(c.RotateZoneAt(c.SpriteToScreen(11, 9) + new Vector(12, 12)));
    }

    [AvaloniaFact]
    public void DraggingAroundTheCorner_TurnsTheBar_ShiftSnaps_OneUndoStep()
    {
        using var h = Setup();
        var from = OutsideCorner(h);
        var centre = h.Window.Canvas.TranslatePoint(h.Window.Canvas.SpriteToScreen(8, 8), h.Window)!.Value;
        // a quarter turn clockwise around the centre: the pointer goes from bottom-right to bottom-left
        var v = from - centre;
        var to = centre + new Vector(-v.Y, v.X);
        h.Window.MouseDown(from, MouseButton.Left, RawInputModifiers.Shift);
        h.Window.MouseMove(to, RawInputModifiers.Shift);
        h.Window.MouseUp(to, MouseButton.Left, RawInputModifiers.Shift);
        Harness.Pump();
        Assert.NotNull(h.Session.Float);
        var b = h.Session.Float!.Bounds;
        Assert.Equal((2, 6), (b.W, b.H)); // the 6x2 bar now stands up
        h.Key(Key.Enter);
        Assert.Equal(Red, h.Pixel(8, 5));
        Assert.Equal(0u, h.Pixel(5, 7));
        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(Red, h.Pixel(5, 7));
    }

    [AvaloniaFact]
    public async Task RotateByAngle_FromTheMenu()
    {
        using var h = Setup();
        h.Dialogs.Numbers.Enqueue(90);
        await h.Window.RotateSelectionByAngleAsync();
        h.Key(Key.Enter);
        Assert.Equal(Red, h.Pixel(8, 5));
    }
}
