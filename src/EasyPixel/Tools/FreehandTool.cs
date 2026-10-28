using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tools;

/// <summary>
/// Tools that paint under the pointer as it drags: pencil, eraser, shading, lighten/darken, dither brush, spray.
/// Each drag is one stroke (one undo step); moves are joined by straight lines so fast drags leave no gaps.
/// </summary>
public sealed class FreehandTool : Tool
{
    private readonly Random _random = new(1);
    private Stroke? _stroke;
    private ToolInput _press;
    private int _lastX, _lastY;
    private readonly List<(int X, int Y)> _path = new(); // pixel-perfect: the points kept so far (image coordinates)
    private uint _value;
    // Shading / lighten-darken: each pixel changes once per stroke, from its value when the stroke began (as in
    // Aseprite), however often overlapping brush stamps pass over it.
    private readonly HashSet<int> _shaded = new();
    private readonly Image _one = new(1, 1);
    private PixelSink? _oneSink;
    private static readonly Brush OnePixel = new();

    public FreehandTool(ToolKind kind, string name) : base(kind, name) { }

    public override void Down(ToolContext c, ToolInput p)
    {
        if (Kind == ToolKind.LightenDarken && c.Session.Doc.Mode == ColorMode.Indexed)
        {
            c.Status(L.T("Lighten / darken works on RGBA sprites; use Shading on indexed ones."));
            return;
        }
        _stroke = Stroke.Begin(c, Name);
        if (_stroke == null) return;
        Active = true;
        _press = p;
        _path.Clear();
        _shaded.Clear();
        _value = Kind == ToolKind.Eraser ? c.Session.Doc.TransparentPixel : PaintValue(c, p);
        _lastX = _stroke.ToImageX(p.X);
        _lastY = _stroke.ToImageY(p.Y);
        Paint(c, new List<(int, int)> { (_lastX, _lastY) });
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (!Active || _stroke == null) return;
        int x = _stroke.ToImageX(p.X), y = _stroke.ToImageY(p.Y);
        if (x == _lastX && y == _lastY) return;
        var pts = LinePoints(_lastX, _lastY, x, y);
        pts.RemoveAt(0); // the start point was painted by the previous step
        _lastX = x;
        _lastY = y;
        Paint(c, pts);
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (Active && _stroke != null) _stroke.Commit();
        _stroke = null;
        Active = false;
    }

    public override void Cancel(ToolContext c)
    {
        _stroke?.Cancel();
        _stroke = null;
        Active = false;
    }

    private void Paint(ToolContext c, List<(int X, int Y)> pts)
    {
        var st = _stroke!;
        var t = c.Tools;
        var sink = st.Step();
        var sym = st.Symmetry;
        var restored = default(PixelRect);
        switch (Kind)
        {
            case ToolKind.Pencil or ToolKind.Eraser when t.PixelPerfect && t.Brush.Size == 1:
                foreach (var pt in pts) restored = restored.Union(PixelPerfectStep(sink, st, pt, sym));
                break;
            case ToolKind.Pencil or ToolKind.Eraser:
                foreach (var (x, y) in pts) Brush.Stamp(sink, x, y, _value, t.Brush, sym);
                break;
            case ToolKind.Shading or ToolKind.LightenDarken:
                foreach (var (x, y) in pts)
                    foreach (var (mx, my) in Mirrors(x, y, sym)) ShadeFootprint(c, st, sink, mx, my);
                break;
            case ToolKind.Dither:
                foreach (var (x, y) in pts)
                    foreach (var (mx, my) in Mirrors(x, y, sym))
                        Brush.ForEach(mx, my, t.Brush, (px, py) =>
                        {
                            if (Core.Raster.Dither.Pattern(t.Dither, px + st.Cel.X, py + st.Cel.Y)) sink.Set(px, py, _value);
                        });
                break;
            case ToolKind.Spray:
                {
                    int r = Math.Max(1, t.SprayRadius);
                    foreach (var (x, y) in pts)
                        for (int i = 0; i < t.SprayDensity; i++)
                        {
                            double a = _random.NextDouble() * Math.PI * 2, d = Math.Sqrt(_random.NextDouble()) * r;
                            Brush.Stamp(sink, x + (int)Math.Round(Math.Cos(a) * d), y + (int)Math.Round(Math.Sin(a) * d), _value, new Brush(), sym);
                        }
                    break;
                }
        }
        st.EndStep(sink, restored);
    }

    /// <summary>
    /// Adds one point to a pixel-perfect stroke: when the previous point turns out to be the corner of an "L", it is
    /// put back to its original value (with its mirrors). Returns the area put back.
    /// </summary>
    private PixelRect PixelPerfectStep(PixelSink sink, Stroke st, (int X, int Y) pt, Symmetry? sym)
    {
        var restored = default(PixelRect);
        if (_path.Count >= 2)
        {
            var (px, py) = _path[^2];
            var (cx, cy) = _path[^1];
            bool corner = Math.Abs(pt.X - px) == 1 && Math.Abs(pt.Y - py) == 1 && (cx == px || cy == py) && (cx == pt.X || cy == pt.Y);
            if (corner)
            {
                foreach (var (mx, my) in Mirrors(cx, cy, sym))
                {
                    st.Restore(new PixelRect(mx, my, 1, 1));
                    restored = restored.Union(new PixelRect(mx, my, 1, 1));
                }
                _path.RemoveAt(_path.Count - 1);
            }
        }
        _path.Add(pt);
        Brush.Stamp(sink, pt.X, pt.Y, _value, new Brush(), sym);
        return restored;
    }

    /// <summary>Shades every pixel under the brush at (x, y) (image coordinates) not shaded yet in this stroke.</summary>
    private void ShadeFootprint(ToolContext c, Stroke st, PixelSink sink, int x, int y)
    {
        int w = st.Image.Width;
        Brush.ForEach(x, y, c.Tools.Brush, (px, py) =>
        {
            if (!sink.CanWrite(px, py) || !_shaded.Add(py * w + px)) return;
            if (Shaded(c, st.Original(px, py)) is uint v) sink.Set(px, py, v);
        });
    }

    /// <summary>The value one shading step away from <paramref name="v"/>, or null when it stays (transparent, not in the ramp, at its end).</summary>
    private uint? Shaded(ToolContext c, uint v)
    {
        var doc = c.Session.Doc;
        bool lighten = !_press.Right;
        if (Kind == ToolKind.LightenDarken)
        {
            if ((v >> 24) == 0) return null;
            _one.Pixels[0] = v;
            _oneSink ??= new PixelSink(_one);
            Shading.LightenDarken(_oneSink, 0, 0, OnePixel, lighten ? c.Tools.LightenAmount : -c.Tools.LightenAmount);
            return _one.Pixels[0];
        }
        int step = lighten ? 1 : -1;
        if (doc.Mode == ColorMode.Indexed)
        {
            int count = doc.Palette.Colors.Count, transparent = doc.Palette.TransparentIndex;
            if (v >= (uint)count || (int)v == transparent) return null;
            int next = (int)v + step;
            return next < 0 || next >= count || next == transparent ? null : (uint)next;
        }
        if ((v >> 24) == 0) return null;
        var ramp = doc.Palette.Colors.Count >= 2 ? doc.Palette : c.Tools.DefaultRamp;
        int idx = ramp.IndexOf(Rgba.FromPacked(v));
        int to = idx + step;
        if (idx < 0 || to < 0 || to >= ramp.Colors.Count || ramp.Colors[to].A == 0) return null;
        return ramp.Colors[to].Packed;
    }

    private static IEnumerable<(int, int)> Mirrors(int x, int y, Symmetry? sym) =>
        sym == null ? new[] { (x, y) } : sym.Mirror(x, y);
}
