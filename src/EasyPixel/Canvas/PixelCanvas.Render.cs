using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EasyPixel.Core.Model;
using EasyPixel.Tools;

namespace EasyPixel.Canvas;

// Drawing: checkerboard, picture, floating pixels, grid, selection outline, tool guides, mirror axes.
public sealed partial class PixelCanvas
{
    private const double MinGridScale = 4; // the pixel grid shows from 400%

    private readonly DispatcherTimer _antsTimer;
    private double _antsPhase;
    private List<(int X0, int Y0, int X1, int Y1)> _selEdges = new(); // selection outline in canvas pixels

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        ctx.FillRectangle(Res.Brush("Viewport"), bounds);
        if (_s == null || _bitmap == null) return;
        var doc = _s.Doc;

        var page = new Rect(Offset.X, Offset.Y, doc.Width * Scale, doc.Height * Scale);
        var vis = page.Intersect(bounds);
        if (vis.Width > 0 && vis.Height > 0)
        {
            ctx.FillRectangle(Checker(), vis);
            using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                DrawPart(ctx, _bitmap, 0, 0, doc.Width, doc.Height, bounds);
                if (FloatBitmap() is { } fb && _s.Float is { } fs)
                    DrawPart(ctx, fb, fs.Pixels.X, fs.Pixels.Y, fb.PixelSize.Width, fb.PixelSize.Height, bounds);
            }
            if (_showGrid && Scale >= MinGridScale) DrawGrid(ctx, vis);
        }
        DrawSelection(ctx, bounds);
        DrawToolOverlay(ctx);
        DrawMirrorAxes(ctx, page);
    }

    /// <summary>Draws the part of a bitmap placed at canvas (x, y) that is inside the control (huge zooms stay cheap).</summary>
    private void DrawPart(DrawingContext ctx, Bitmap bmp, int x, int y, int w, int h, Rect bounds)
    {
        var dest = new Rect(Offset.X + x * Scale, Offset.Y + y * Scale, w * Scale, h * Scale);
        var vis = dest.Intersect(bounds);
        if (vis.Width <= 0 || vis.Height <= 0) return;
        int sx0 = Math.Clamp((int)Math.Floor((vis.X - dest.X) / Scale), 0, w);
        int sy0 = Math.Clamp((int)Math.Floor((vis.Y - dest.Y) / Scale), 0, h);
        int sx1 = Math.Clamp((int)Math.Ceiling((vis.Right - dest.X) / Scale), 0, w);
        int sy1 = Math.Clamp((int)Math.Ceiling((vis.Bottom - dest.Y) / Scale), 0, h);
        if (sx1 <= sx0 || sy1 <= sy0) return;
        ctx.DrawImage(bmp, new Rect(sx0, sy0, sx1 - sx0, sy1 - sy0),
            new Rect(dest.X + sx0 * Scale, dest.Y + sy0 * Scale, (sx1 - sx0) * Scale, (sy1 - sy0) * Scale));
    }

    private ImageBrush Checker()
    {
        if (_checker != null) return _checker;
        const int cell = 8;
        var bmp = new WriteableBitmap(new PixelSize(cell * 2, cell * 2), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        uint a = Packed(Res.Color("CheckerA")), b = Packed(Res.Color("CheckerB"));
        using (var fb = bmp.Lock())
        {
            unsafe
            {
                for (int y = 0; y < cell * 2; y++)
                {
                    var row = new Span<uint>((byte*)fb.Address + y * fb.RowBytes, cell * 2);
                    for (int x = 0; x < cell * 2; x++) row[x] = (x / cell + y / cell) % 2 == 0 ? a : b;
                }
            }
        }
        _checker = new ImageBrush(bmp)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            DestinationRect = new RelativeRect(0, 0, cell * 2, cell * 2, RelativeUnit.Absolute),
        };
        return _checker;
    }

    private static uint Packed(Color c) => new Rgba(c.R, c.G, c.B, c.A).Packed;

    private void DrawGrid(DrawingContext ctx, Rect vis)
    {
        var pen = new Pen(Res.Brush("GridLine"), 1);
        int x0 = (int)Math.Ceiling((vis.X - Offset.X) / Scale), x1 = (int)Math.Floor((vis.Right - Offset.X) / Scale);
        int y0 = (int)Math.Ceiling((vis.Y - Offset.Y) / Scale), y1 = (int)Math.Floor((vis.Bottom - Offset.Y) / Scale);
        for (int x = x0; x <= x1; x++)
        {
            double sx = Math.Round(Offset.X + x * Scale) + 0.5;
            ctx.DrawLine(pen, new Point(sx, vis.Y), new Point(sx, vis.Bottom));
        }
        for (int y = y0; y <= y1; y++)
        {
            double sy = Math.Round(Offset.Y + y * Scale) + 0.5;
            ctx.DrawLine(pen, new Point(vis.X, sy), new Point(vis.Right, sy));
        }
    }

    private void OnSelectionChanged()
    {
        _selEdges = _s == null ? new() : Outline(_s.Float?.SelectionAtLift ?? _s.Sel);
        bool any = _selEdges.Count > 0;
        if (any && !_antsTimer.IsEnabled) _antsTimer.Start();
        else if (!any) _antsTimer.Stop();
        InvalidateVisual();
    }

    /// <summary>The selection's border as horizontal and vertical runs of pixel edges.</summary>
    private static List<(int, int, int, int)> Outline(Core.Select.Selection sel)
    {
        var edges = new List<(int, int, int, int)>();
        var b = sel.Bounds;
        if (b.IsEmpty) return edges;
        for (int y = b.Y; y <= b.Bottom; y++)
        {
            int start = -1;
            for (int x = b.X; x <= b.Right; x++)
            {
                bool edge = x < b.Right && sel.Contains(x, y - 1) != sel.Contains(x, y);
                if (edge && start < 0) start = x;
                else if (!edge && start >= 0) { edges.Add((start, y, x, y)); start = -1; }
            }
        }
        for (int x = b.X; x <= b.Right; x++)
        {
            int start = -1;
            for (int y = b.Y; y <= b.Bottom; y++)
            {
                bool edge = y < b.Bottom && sel.Contains(x - 1, y) != sel.Contains(x, y);
                if (edge && start < 0) start = y;
                else if (!edge && start >= 0) { edges.Add((x, start, x, y)); start = -1; }
            }
        }
        return edges;
    }

    private void DrawSelection(DrawingContext ctx, Rect bounds)
    {
        if (_selEdges.Count == 0 || _s == null) return;
        int dx = 0, dy = 0;
        if (_s.Float is { } fs) { dx = fs.Pixels.X - fs.StartX; dy = fs.Pixels.Y - fs.StartY; }
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            foreach (var (x0, y0, x1, y1) in _selEdges)
            {
                var a = SpriteToScreen(x0 + dx, y0 + dy);
                var b = SpriteToScreen(x1 + dx, y1 + dy);
                if (Math.Max(a.X, b.X) < bounds.X - 1 || Math.Min(a.X, b.X) > bounds.Right + 1
                    || Math.Max(a.Y, b.Y) < bounds.Y - 1 || Math.Min(a.Y, b.Y) > bounds.Bottom + 1) continue;
                g.BeginFigure(Snap(a), false);
                g.LineTo(Snap(b));
                g.EndFigure(false);
            }
        }
        DrawAnts(ctx, geo);
    }

    private void DrawAnts(DrawingContext ctx, Geometry geo)
    {
        ctx.DrawGeometry(null, new Pen(Res.Brush("AntsLight"), 1), geo);
        ctx.DrawGeometry(null, new Pen(Res.Brush("AntsDark"), 1, new DashStyle(new double[] { 4, 4 }, _antsPhase)), geo);
    }

    private static Point Snap(Point p) => new(Math.Round(p.X) + 0.5, Math.Round(p.Y) + 0.5);

    private void DrawToolOverlay(DrawingContext ctx)
    {
        if (_activeTool?.Overlay is not { } o || o.Points.Count == 0) return;
        Geometry geo;
        if (o.Shape == OverlayShape.Polyline)
        {
            var sg = new StreamGeometry();
            using (var g = sg.Open())
            {
                g.BeginFigure(Snap(SpriteToScreen(o.Points[0].X + 0.5, o.Points[0].Y + 0.5)), false);
                foreach (var (x, y) in o.Points.Skip(1)) g.LineTo(Snap(SpriteToScreen(x + 0.5, y + 0.5)));
                g.EndFigure(false);
            }
            geo = sg;
        }
        else
        {
            var (x0, y0) = o.Points[0];
            var (x1, y1) = o.Points[^1];
            var a = SpriteToScreen(Math.Min(x0, x1), Math.Min(y0, y1));
            var b = SpriteToScreen(Math.Max(x0, x1) + 1, Math.Max(y0, y1) + 1);
            var r = new Rect(a, b);
            geo = o.Shape == OverlayShape.Rect ? new RectangleGeometry(r) : new EllipseGeometry(r);
        }
        DrawAnts(ctx, geo);
    }

    private void DrawMirrorAxes(DrawingContext ctx, Rect page)
    {
        if (_tools == null || !(_tools.MirrorH || _tools.MirrorV)) return;
        var pen = new Pen(Res.Brush("Accent"), 1, new DashStyle(new double[] { 6, 4 }, 0));
        if (_tools.MirrorH)
        {
            double x = Math.Round(page.X + page.Width / 2) + 0.5;
            ctx.DrawLine(pen, new Point(x, page.Y), new Point(x, page.Bottom));
        }
        if (_tools.MirrorV)
        {
            double y = Math.Round(page.Y + page.Height / 2) + 0.5;
            ctx.DrawLine(pen, new Point(page.X, y), new Point(page.Right, y));
        }
    }
}
