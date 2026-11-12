using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel.Tools;

/// <summary>
/// Dragging just outside a corner of the selection (with a selection tool or Move) turns the selected pixels around
/// their centre by any angle, without blending, always from the original pixels so turning back and forth loses
/// nothing. Shift snaps to steps of 15°. They stay floating: move them, Enter drops them (one undo step); Escape during
/// the drag puts them back.
/// </summary>
public sealed class RotateTool : Tool
{
    public const double SnapDegrees = 15;

    private PixelRect _start;
    private Image? _orig;
    private int _origX, _origY, _origStartX, _origStartY;
    private Selection? _origSel;
    private double _startAngle;

    public RotateTool() : base(ToolKind.Move, "Rotate") { }

    /// <summary>The angle of the last drag, in degrees (for the status bar).</summary>
    public double Angle { get; private set; }

    public override void Down(ToolContext c, ToolInput p)
    {
        var s = c.Session;
        if (s.Float == null)
        {
            if (s.WhyNotEditable() is { } why) { c.Status(why); return; }
            if (s.Sel.IsEmpty) return;
            s.LiftSelection(L.N("Rotate"));
        }
        var fs = s.Float!;
        _start = fs.Bounds;
        _orig = fs.Pixels.Pixels;
        _origX = fs.Pixels.X;
        _origY = fs.Pixels.Y;
        _origStartX = fs.StartX;
        _origStartY = fs.StartY;
        _origSel = fs.SelectionAtLift;
        _startAngle = PointerAngle(p);
        Angle = 0;
        Active = true;
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Apply(c.Session, TargetAngle(p));
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Move(c, p);
        Active = false;
    }

    public override void Cancel(ToolContext c)
    {
        if (Active) Apply(c.Session, 0);
        Active = false;
    }

    /// <summary>How far the pointer has turned around the box's centre since the press (Shift: steps of 15°).</summary>
    internal double TargetAngle(ToolInput p)
    {
        double a = PointerAngle(p) - _startAngle;
        a = ((a % 360) + 540) % 360 - 180; // -180..180
        return p.Shift ? Math.Round(a / SnapDegrees) * SnapDegrees : Math.Round(a);
    }

    private double PointerAngle(ToolInput p)
    {
        double cx = _start.X + _start.W / 2.0, cy = _start.Y + _start.H / 2.0;
        return Math.Atan2(p.Y + 0.5 - cy, p.X + 0.5 - cx) * 180 / Math.PI;
    }

    private void Apply(EditorSession s, double angle)
    {
        if (s.Float is not { } fs || _orig == null || angle == Angle) return;
        Angle = angle;
        fs.Pixels.Restore(_orig, _origX, _origY);
        fs.SelectionAtLift = _origSel!;
        fs.StartX = _origStartX;
        fs.StartY = _origStartY;
        s.TransformFloating(f => f.RotateBy(angle));
    }
}
