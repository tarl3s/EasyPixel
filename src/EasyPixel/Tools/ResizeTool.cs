using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel.Tools;

/// <summary>Which of the eight handles around a selection is held: -1 / 0 / +1 per side (0 = that axis doesn't change).</summary>
public readonly record struct ResizeHandle(int Sx, int Sy);

/// <summary>
/// Dragging a handle of the selection (with a selection tool or Move): the selected pixels are lifted and stretched
/// (nearest neighbour) to the new box, always from the original pixels, so dragging back and forth loses nothing.
/// Shift keeps the proportions. They stay floating: move them, Enter drops them (one undo step); Escape during the drag
/// puts the box back.
/// </summary>
public sealed class ResizeTool : Tool
{
    private ResizeHandle _h;
    private PixelRect _start;
    private Image? _orig;
    private int _origX, _origY, _origStartX, _origStartY;
    private Selection? _origSel;

    public ResizeTool() : base(ToolKind.Move, "Resize") { }

    public ResizeHandle Handle { set => _h = value; }

    /// <summary>The box the handles go around: the floating pixels, or the selection.</summary>
    public static PixelRect? Box(EditorSession s) =>
        s.Float is { } fs ? fs.Bounds : s.Sel.IsEmpty ? null : s.Sel.Bounds;

    public override void Down(ToolContext c, ToolInput p)
    {
        var s = c.Session;
        if (s.Float == null)
        {
            if (s.WhyNotEditable() is { } why) { c.Status(why); return; }
            if (s.Sel.IsEmpty) return;
            s.LiftSelection(L.N("Resize"));
        }
        var fs = s.Float!;
        _start = fs.Bounds;
        _orig = fs.Pixels.Pixels;
        _origX = fs.Pixels.X;
        _origY = fs.Pixels.Y;
        _origStartX = fs.StartX;
        _origStartY = fs.StartY;
        _origSel = fs.SelectionAtLift;
        Active = true;
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Apply(c.Session, Target(p));
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Move(c, p);
        Active = false;
    }

    public override void Cancel(ToolContext c)
    {
        if (Active) Apply(c.Session, _start);
        Active = false;
    }

    /// <summary>The new box for the pointer at canvas pixel (x, y): the held sides follow it, the others stay.</summary>
    internal PixelRect Target(ToolInput p)
    {
        int l = _start.X, t = _start.Y, r = _start.Right, b = _start.Bottom;
        if (_h.Sx < 0) l = Math.Min(p.X, r - 1);
        if (_h.Sx > 0) r = Math.Max(p.X + 1, l + 1);
        if (_h.Sy < 0) t = Math.Min(p.Y, b - 1);
        if (_h.Sy > 0) b = Math.Max(p.Y + 1, t + 1);
        int w = r - l, h = b - t;
        if (p.Shift && _start.W > 0 && _start.H > 0)
        {
            // keep the proportions: the side that moved the most decides the size
            double k = _h.Sx == 0 ? (double)h / _start.H : _h.Sy == 0 ? (double)w / _start.W
                : Math.Max((double)w / _start.W, (double)h / _start.H);
            w = Math.Max(1, (int)Math.Round(_start.W * k));
            h = Math.Max(1, (int)Math.Round(_start.H * k));
            if (_h.Sx < 0) l = r - w; else if (_h.Sx == 0) l = _start.X + (_start.W - w) / 2;
            if (_h.Sy < 0) t = b - h; else if (_h.Sy == 0) t = _start.Y + (_start.H - h) / 2;
        }
        return new PixelRect(l, t, w, h);
    }

    private void Apply(EditorSession s, PixelRect box)
    {
        if (s.Float is not { } fs || _orig == null) return;
        if (fs.Bounds == box && fs.Pixels.Pixels.Width == box.W && fs.Pixels.Pixels.Height == box.H) return;
        fs.Pixels.Restore(_orig, _origX, _origY);
        fs.SelectionAtLift = _origSel!;
        fs.StartX = _origStartX;
        fs.StartY = _origStartY;
        s.TransformFloating(f => f.ScaleTo(box));
    }
}
