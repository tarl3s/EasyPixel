using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using EasyPixel.Tools;

namespace EasyPixel.Canvas;

// Resize handles: with a selection tool or Move, eight small squares around the selection (or the floating pixels);
// dragging one stretches the selected pixels (ResizeTool).
public sealed partial class PixelCanvas
{
    private const double HandleSize = 7, HandleReach = 6;
    private readonly ResizeTool _resize = new();

    private static readonly ResizeHandle[] AllHandles =
    {
        new(-1, -1), new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0),
    };

    private bool HandlesShown =>
        _s != null && _tools != null && _tools.CurrentTool.Kind is ToolKind.RectSelect or ToolKind.EllipseSelect or ToolKind.Lasso or ToolKind.MagicWand or ToolKind.Move
        && (_activeTool == null || ReferenceEquals(_activeTool, _resize)) && ResizeTool.Box(_s) != null;

    /// <summary>The handle's centre on screen.</summary>
    private Point HandlePoint(Core.Model.PixelRect box, ResizeHandle h)
    {
        var a = SpriteToScreen(box.X, box.Y);
        var b = SpriteToScreen(box.Right, box.Bottom);
        return new Point(h.Sx < 0 ? a.X : h.Sx > 0 ? b.X : (a.X + b.X) / 2, h.Sy < 0 ? a.Y : h.Sy > 0 ? b.Y : (a.Y + b.Y) / 2);
    }

    /// <summary>The handle under the pointer, if any.</summary>
    internal ResizeHandle? HandleAt(Point pos)
    {
        if (!HandlesShown) return null;
        var box = ResizeTool.Box(_s!)!.Value;
        foreach (var h in AllHandles)
        {
            var c = HandlePoint(box, h);
            if (Math.Abs(pos.X - c.X) <= HandleReach && Math.Abs(pos.Y - c.Y) <= HandleReach) return h;
        }
        return null;
    }

    /// <summary>Starts a resize when the press is on a handle. True when it did.</summary>
    private bool TryStartResize(Point pos, KeyModifiers m)
    {
        if (HandleAt(pos) is not { } h || _ctx == null) return false;
        _resize.Handle = h;
        _lastInput = Input(pos, m, false);
        _resize.Down(_ctx, _lastInput);
        if (!_resize.Active) return false;
        _activeTool = _resize;
        return true;
    }

    private void UpdateHandleCursor(Point pos)
    {
        if (ReferenceEquals(_activeTool, _resize)) return; // keeps the cursor it had when the drag began
        Cursor = HandleAt(pos) is not { } r ? Cursor.Default : new Cursor((r.Sx, r.Sy) switch
        {
            (-1, -1) => StandardCursorType.TopLeftCorner,
            (1, -1) => StandardCursorType.TopRightCorner,
            (-1, 1) => StandardCursorType.BottomLeftCorner,
            (1, 1) => StandardCursorType.BottomRightCorner,
            (0, _) => r.Sy < 0 ? StandardCursorType.TopSide : StandardCursorType.BottomSide,
            _ => r.Sx < 0 ? StandardCursorType.LeftSide : StandardCursorType.RightSide,
        });
    }

    private void DrawHandles(DrawingContext ctx)
    {
        if (!HandlesShown) return;
        var box = ResizeTool.Box(_s!)!.Value;
        var fill = Res.Brush("AntsLight");
        var pen = new Pen(Res.Brush("AntsDark"), 1);
        foreach (var h in AllHandles)
        {
            var c = HandlePoint(box, h);
            var r = new Rect(Math.Round(c.X - HandleSize / 2) + 0.5, Math.Round(c.Y - HandleSize / 2) + 0.5, HandleSize - 1, HandleSize - 1);
            ctx.FillRectangle(fill, r);
            ctx.DrawRectangle(pen, r);
        }
    }
}
