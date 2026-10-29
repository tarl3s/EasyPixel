using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Tools;

namespace EasyPixel.UiTests;

public class GuideTests
{
    /// <summary>Dragging a marquee or a lasso (and cancelling one) only repaints the guide: the picture is not recomposited.</summary>
    [AvaloniaFact]
    public void MarqueeDrag_4096_DoesNotRecomposite()
    {
        using var h = new Harness(4096, 4096);
        var canvas = h.Window.Canvas;
        foreach (var kind in new[] { ToolKind.RectSelect, ToolKind.EllipseSelect, ToolKind.Lasso })
        {
            h.Window.Tools.Current = kind;
            h.Session.Sel = Core.Select.Selection.None(4096, 4096); // a press on the old selection's corner would grab its resize handle
            long before = canvas.RefreshedPixels;
            h.Drag((10, 10), (300, 80), (900, 700), (1200, 1500));
            Assert.Equal(before, canvas.RefreshedPixels);
            Assert.False(h.Session.Sel.IsEmpty);
        }

        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Session.Sel = Core.Select.Selection.None(4096, 4096);
        h.Window.MouseDown(h.At(5, 5), MouseButton.Left, RawInputModifiers.None);
        h.Window.MouseMove(h.At(50, 50), RawInputModifiers.None);
        long mid = canvas.RefreshedPixels;
        h.Key(Key.Escape);
        Assert.Equal(mid, canvas.RefreshedPixels);
        h.Window.MouseUp(h.At(50, 50), MouseButton.Left, RawInputModifiers.None);
    }
}
