namespace EasyPixel.Core.Raster;

/// <summary>Lines, rectangles and ellipses. Outlines stamp the brush along the shape; fills ignore the brush.</summary>
public static class Shapes
{
    public static void Line(PixelSink s, uint color, int x0, int y0, int x1, int y1, Brush b, bool pixelPerfect, Symmetry? sym = null)
    {
        var pts = LinePoints(x0, y0, x1, y1);
        if (pixelPerfect) pts = PixelPerfect(pts);
        foreach (var (x, y) in pts) Brush.Stamp(s, x, y, color, b, sym);
    }

    public static void Rect(PixelSink s, uint color, int x0, int y0, int x1, int y1, Brush b, bool filled, Symmetry? sym = null)
    {
        int l = Math.Min(x0, x1), r = Math.Max(x0, x1), t = Math.Min(y0, y1), bt = Math.Max(y0, y1);
        if (filled)
        {
            for (int y = t; y <= bt; y++)
                for (int x = l; x <= r; x++) PlotFill(s, x, y, color, sym);
            return;
        }
        for (int x = l; x <= r; x++) { Brush.Stamp(s, x, t, color, b, sym); Brush.Stamp(s, x, bt, color, b, sym); }
        for (int y = t + 1; y < bt; y++) { Brush.Stamp(s, l, y, color, b, sym); Brush.Stamp(s, r, y, color, b, sym); }
    }

    /// <summary>Ellipse inscribed in the box (x0,y0)-(x1,y1), both corners included.</summary>
    public static void Ellipse(PixelSink s, uint color, int x0, int y0, int x1, int y1, Brush b, bool filled, Symmetry? sym = null)
    {
        int l = Math.Min(x0, x1), t = Math.Min(y0, y1);
        int w = Math.Abs(x1 - x0) + 1, h = Math.Abs(y1 - y0) + 1;
        var mask = new bool[w * h];
        double rx = w / 2.0, ry = h / 2.0;
        for (int j = 0; j < h; j++)
        {
            double dy = (j + 0.5 - ry) / ry;
            for (int i = 0; i < w; i++)
            {
                double dx = (i + 0.5 - rx) / rx;
                mask[j * w + i] = dx * dx + dy * dy <= 1.0;
            }
        }
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                if (!mask[j * w + i]) continue;
                if (filled) { PlotFill(s, l + i, t + j, color, sym); continue; }
                bool edge = i == 0 || j == 0 || i == w - 1 || j == h - 1
                    || !mask[j * w + i - 1] || !mask[j * w + i + 1] || !mask[(j - 1) * w + i] || !mask[(j + 1) * w + i];
                if (edge) Brush.Stamp(s, l + i, t + j, color, b, sym);
            }
    }

    private static void PlotFill(PixelSink s, int x, int y, uint color, Symmetry? sym)
    {
        if (sym == null) { s.Set(x, y, color); return; }
        Span<(int, int)> pts = stackalloc (int, int)[4];
        int n = sym.Expand(x, y, pts);
        for (int k = 0; k < n; k++) s.Set(pts[k].Item1, pts[k].Item2, color);
    }

    internal static List<(int, int)> LinePoints(int x0, int y0, int x1, int y1)
    {
        var pts = new List<(int, int)>();
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            pts.Add((x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return pts;
    }

    /// <summary>Drops the middle pixel of every "L" corner (three points where the middle one only joins two orthogonal steps).</summary>
    public static List<(int, int)> PixelPerfect(List<(int, int)> pts)
    {
        if (pts.Count < 3) return pts;
        var res = new List<(int, int)> { pts[0] };
        for (int i = 1; i < pts.Count - 1; i++)
        {
            var (px, py) = res[^1];
            var (cx, cy) = pts[i];
            var (nx, ny) = pts[i + 1];
            bool corner = Math.Abs(nx - px) == 1 && Math.Abs(ny - py) == 1 && (cx == px || cy == py) && (cx == nx || cy == ny);
            if (!corner) res.Add(pts[i]);
        }
        res.Add(pts[^1]);
        return res;
    }
}
