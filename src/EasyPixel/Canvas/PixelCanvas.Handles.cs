using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using EasyPixel.Tools;

namespace EasyPixel.Canvas;

// Resize handles: with a selection tool or Move, eight small squares around the selection (or the floating pixels);
// dragging one stretches the selected pixels (ResizeTool); dragging just outside a corner turns them (RotateTool).
public sealed partial class PixelCanvas
{
    private const double HandleSize = 7, HandleReach = 6, RotateReach = 22;
    private readonly ResizeTool _resize = new();
    private readonly RotateTool _rotate = new();

    /// <summary>The angle of the turn being dragged, or null when no turn is being dragged (for the status bar).</summary>
    public double? RotatingAngle => ReferenceEquals(_activeTool, _rotate) ? _rotate.Angle : null;

    private static readonly ResizeHandle[] AllHandles =
    {
        new(-1, -1), new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0),
    };

    private bool HandlesShown =>
        _s != null && _tools != null && _tools.CurrentTool.Kind is ToolKind.RectSelect or ToolKind.EllipseSelect or ToolKind.Lasso or ToolKind.MagicWand or ToolKind.Move
        && (_activeTool == null || ReferenceEquals(_activeTool, _resize) || ReferenceEquals(_activeTool, _rotate)) && ResizeTool.Box(_s) != null;

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

    /// <summary>
    /// True when the pointer is just outside a corner of the box (beyond its handle, within <see cref="RotateReach"/>
    /// screen pixels): pressing there turns the selection.
    /// </summary>
    internal bool RotateZoneAt(Point pos)
    {
        if (!HandlesShown || HandleAt(pos) != null) return false;
        var box = ResizeTool.Box(_s!)!.Value;
        var a = SpriteToScreen(box.X, box.Y);
        var b = SpriteToScreen(box.Right, box.Bottom);
        if (pos.X > a.X && pos.X < b.X && pos.Y > a.Y && pos.Y < b.Y) return false; // inside: that's moving
        foreach (var h in AllHandles)
        {
            if (h.Sx == 0 || h.Sy == 0) continue; // corners only
            var c = HandlePoint(box, h);
            if (Math.Abs(pos.X - c.X) <= RotateReach && Math.Abs(pos.Y - c.Y) <= RotateReach) return true;
        }
        return false;
    }

    /// <summary>Starts a resize (press on a handle) or a turn (press just outside a corner). True when it did.</summary>
    private bool TryStartResize(Point pos, KeyModifiers m)
    {
        if (_ctx == null) return false;
        Tool tool;
        if (HandleAt(pos) is { } h) { _resize.Handle = h; tool = _resize; }
        else if (RotateZoneAt(pos)) tool = _rotate;
        else return false;
        _lastInput = Input(pos, m, false);
        tool.Down(_ctx, _lastInput);
        if (!tool.Active) return false;
        _activeTool = tool;
        return true;
    }

    private void UpdateHandleCursor(Point pos)
    {
        if (ReferenceEquals(_activeTool, _resize) || ReferenceEquals(_activeTool, _rotate)) return; // keeps the cursor it had when the drag began
        if (RotateZoneAt(pos)) { Cursor = new Cursor(StandardCursorType.Hand); return; }
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
