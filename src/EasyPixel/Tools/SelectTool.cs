using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel.Tools;

/// <summary>
/// Rectangle, ellipse and lasso selection. Shift adds to the selection, Alt subtracts; a plain click without a drag
/// clears it.
/// </summary>
public sealed class SelectTool : Tool
{
    private readonly List<(int X, int Y)> _points = new();
    private SelectMode _mode;

    public SelectTool(ToolKind kind)
        : base(kind, kind switch { ToolKind.RectSelect => "Rectangle selection", ToolKind.EllipseSelect => "Ellipse selection", _ => "Lasso" }) { }

    public override ToolOverlay? Overlay => !Active || _points.Count == 0 ? null
        : Kind == ToolKind.Lasso ? new ToolOverlay(OverlayShape.Polyline, _points.ToArray())
        : new ToolOverlay(Kind == ToolKind.RectSelect ? OverlayShape.Rect : OverlayShape.Ellipse, new[] { _points[0], _points[^1] });

    public override void Down(ToolContext c, ToolInput p)
    {
        c.Session.DropFloating();
        Active = true;
        _mode = Mode(p);
        _points.Clear();
        _points.Add((p.X, p.Y));
        if (Kind != ToolKind.Lasso) _points.Add((p.X, p.Y));
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        if (Kind == ToolKind.Lasso) { if (_points[^1] != (p.X, p.Y)) _points.Add((p.X, p.Y)); }
        else _points[^1] = (p.X, p.Y);
        // Only the guide moved: the canvas repaints it; no pixels changed, so nothing is recomposited.
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Move(c, p);
        Active = false;
        var s = c.Session;
        int w = s.Doc.Width, h = s.Doc.Height;
        bool click = _points.Distinct().Count() == 1;
        if (click && _mode == SelectMode.Replace)
        {
            s.Sel = Selection.None(w, h);
            return;
        }
        Selection shape;
        if (Kind == ToolKind.Lasso) shape = Selectors.Lasso(w, h, _points);
        else
        {
            var (x0, y0) = _points[0];
            var (x1, y1) = _points[^1];
            var r = new PixelRect(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0) + 1, Math.Abs(y1 - y0) + 1);
            shape = Kind == ToolKind.RectSelect ? Selectors.Rect(w, h, r) : Selectors.Ellipse(w, h, r);
        }
        s.Sel = SelectionTools.Combine(s.Sel, shape, _mode);
        _points.Clear();
    }

    public override void Cancel(ToolContext c)
    {
        Active = false;
        _points.Clear(); // the canvas repaints without the guide
    }

    internal static SelectMode Mode(ToolInput p) => p.Shift ? SelectMode.Add : p.Alt ? SelectMode.Subtract : SelectMode.Replace;
}

/// <summary>Selects the pixels of the current layer that match the clicked one (tolerance, contiguous); Shift adds, Alt subtracts.</summary>
public sealed class MagicWandTool : Tool
{
    public MagicWandTool() : base(ToolKind.MagicWand, "Magic wand") { }

    public override void Down(ToolContext c, ToolInput p)
    {
        var s = c.Session;
        s.DropFloating();
        int w = s.Doc.Width, h = s.Doc.Height;
        Selection shape;
        if (s.Doc.Cels.TryGetValue((s.Layer, s.Frame), out var cel))
        {
            var inImage = Selectors.MagicWand(cel.Image, p.X - cel.X, p.Y - cel.Y, c.Tools.Tolerance, c.Tools.Contiguous);
            shape = inImage.Offset(cel.X, cel.Y, w, h);
        }
        else shape = Selection.All(w, h); // an empty cel: everything is the same (transparent)
        s.Sel = SelectionTools.Combine(s.Sel, shape, SelectTool.Mode(p));
    }
}
