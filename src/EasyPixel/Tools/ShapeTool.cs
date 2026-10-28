using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tools;

/// <summary>
/// Line, rectangle and ellipse: dragged from the press point, previewed in the picture and kept on release.
/// Shift makes a square / circle, or snaps a line to 45 degrees. Filled or outlined per <see cref="ToolController.Filled"/>.
/// </summary>
public sealed class ShapeTool : Tool
{
    private Stroke? _stroke;
    private int _x0, _y0;
    private uint _value;

    public ShapeTool(ToolKind kind, string name) : base(kind, name) { }

    public override void Down(ToolContext c, ToolInput p)
    {
        _stroke = Stroke.Begin(c, Name);
        if (_stroke == null) return;
        Active = true;
        _value = PaintValue(c, p);
        _x0 = _stroke.ToImageX(p.X);
        _y0 = _stroke.ToImageY(p.Y);
        Draw(c, p);
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (Active && _stroke != null) Draw(c, p);
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (Active && _stroke != null)
        {
            Draw(c, p);
            _stroke.Commit();
        }
        _stroke = null;
        Active = false;
    }

    public override void Cancel(ToolContext c)
    {
        _stroke?.Cancel();
        _stroke = null;
        Active = false;
    }

    private void Draw(ToolContext c, ToolInput p)
    {
        var st = _stroke!;
        var t = c.Tools;
        int x1 = st.ToImageX(p.X), y1 = st.ToImageY(p.Y);
        if (p.Shift) (x1, y1) = Constrain(x1, y1);
        st.RestoreAll();
        var sink = st.Step();
        switch (Kind)
        {
            case ToolKind.Line: Shapes.Line(sink, _value, _x0, _y0, x1, y1, t.Brush, false, st.Symmetry); break;
            case ToolKind.Rectangle: Shapes.Rect(sink, _value, _x0, _y0, x1, y1, t.Brush, t.Filled, st.Symmetry); break;
            default: Shapes.Ellipse(sink, _value, _x0, _y0, x1, y1, t.Brush, t.Filled, st.Symmetry); break;
        }
        st.EndStep(sink);
    }

    /// <summary>Shift: equal sides for boxes; for lines the nearest of horizontal, vertical or diagonal.</summary>
    private (int, int) Constrain(int x1, int y1)
    {
        int dx = x1 - _x0, dy = y1 - _y0;
        if (Kind == ToolKind.Line)
        {
            if (Math.Abs(dx) > 2 * Math.Abs(dy)) return (x1, _y0);
            if (Math.Abs(dy) > 2 * Math.Abs(dx)) return (_x0, y1);
        }
        int n = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return (_x0 + Math.Sign(dx == 0 ? 1 : dx) * n, _y0 + Math.Sign(dy == 0 ? 1 : dy) * n);
    }
}

/// <summary>Bucket fill: the touching region (or every matching pixel) within the tolerance, clipped to the selection.</summary>
public sealed class BucketTool : Tool
{
    public BucketTool() : base(ToolKind.Bucket, "Bucket") { }

    public override void Down(ToolContext c, ToolInput p)
    {
        var st = Stroke.Begin(c, Name);
        if (st == null) return;
        var sink = st.Step();
        Fill.Flood(sink, st.ToImageX(p.X), st.ToImageY(p.Y), PaintValue(c, p), c.Tools.Contiguous, c.Tools.Tolerance);
        st.EndStep(sink);
        st.Commit();
    }
}

/// <summary>Picks the shown colour under the pointer: primary with the left button, secondary with the right.</summary>
public sealed class EyedropperTool : Tool
{
    public EyedropperTool() : base(ToolKind.Eyedropper, "Eyedropper") { }

    public override void Down(ToolContext c, ToolInput p) { Active = true; Pick(c, p); }
    public override void Move(ToolContext c, ToolInput p) { if (Active) Pick(c, p); }

    private static void Pick(ToolContext c, ToolInput p)
    {
        if (!c.Session.CanvasRect.Contains(p.X, p.Y)) return;
        var col = c.PickComposite(p.X, p.Y);
        if (p.Right) c.Tools.SetSecondary(col); else c.Tools.SetPrimary(col);
    }
}
