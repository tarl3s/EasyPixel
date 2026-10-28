using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Select;

/// <summary>Builds selections from shapes and from the picture. All coordinates are canvas pixels.</summary>
public static class Selectors
{
    public static Selection Rect(int canvasW, int canvasH, PixelRect r)
    {
        var s = new Selection(canvasW, canvasH);
        r = r.Intersect(new PixelRect(0, 0, canvasW, canvasH));
        for (int y = r.Y; y < r.Bottom; y++)
            for (int x = r.X; x < r.Right; x++) s.Set(x, y, true);
        return s;
    }

    /// <summary>The ellipse inscribed in <paramref name="r"/>; a pixel is in when its centre is.</summary>
    public static Selection Ellipse(int canvasW, int canvasH, PixelRect r)
    {
        var s = new Selection(canvasW, canvasH);
        if (r.IsEmpty) return s;
        double cx = r.X + r.W / 2.0, cy = r.Y + r.H / 2.0, rx = r.W / 2.0, ry = r.H / 2.0;
        var v = r.Intersect(new PixelRect(0, 0, canvasW, canvasH));
        for (int y = v.Y; y < v.Bottom; y++)
            for (int x = v.X; x < v.Right; x++)
            {
                double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy <= 1.0) s.Set(x, y, true);
            }
        return s;
    }

    /// <summary>Even-odd polygon fill (vertices are pixel positions), plus the outline pixels.</summary>
    public static Selection Lasso(int canvasW, int canvasH, IReadOnlyList<(int X, int Y)> poly)
    {
        var s = new Selection(canvasW, canvasH);
        int n = poly.Count;
        if (n == 0) return s;
        var xs = new List<double>();
        int minY = poly.Min(p => p.Y), maxY = poly.Max(p => p.Y);
        for (int y = Math.Max(0, minY); y <= Math.Min(canvasH - 1, maxY); y++)
        {
            double yc = y + 0.5;
            xs.Clear();
            for (int i = 0; i < n; i++)
            {
                var (x1, y1) = poly[i];
                var (x2, y2) = poly[(i + 1) % n];
                double ay = y1 + 0.5, by = y2 + 0.5;
                if ((ay <= yc) == (by <= yc)) continue;
                xs.Add(x1 + 0.5 + (yc - ay) * (x2 - x1) / (by - ay));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
            {
                int from = (int)Math.Ceiling(xs[k] - 0.5), to = (int)Math.Floor(xs[k + 1] - 0.5);
                for (int x = Math.Max(0, from); x <= Math.Min(canvasW - 1, to); x++) s.Set(x, y, true);
            }
        }
        for (int i = 0; i < n; i++)
        {
            var (x1, y1) = poly[i];
            var (x2, y2) = poly[(i + 1) % n];
            Line(s, x1, y1, x2, y2);
        }
        return s;
    }

    private static void Line(Selection s, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            s.Set(x0, y0, true);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Selects every pixel of the image whose value matches <paramref name="value"/> within <paramref name="tolerance"/> (same rule as the bucket fill).</summary>
    public static Selection ByValue(Image img, uint value, int tolerance)
    {
        int w = img.Width, h = img.Height;
        var s = new Selection(w, h);
        tolerance = Math.Clamp(tolerance, 0, 255);
        var px = img.Pixels;
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                if (Fill.Matches(px[j * w + i], value, tolerance)) s.Set(i, j, true);
        return s;
    }

    /// <summary>Selects pixels matching the one at (x, y) within <paramref name="tolerance"/> (same rule as the bucket fill).</summary>
    public static Selection MagicWand(Image img, int x, int y, int tolerance, bool contiguous)
    {
        int w = img.Width, h = img.Height;
        var s = new Selection(w, h);
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return s;
        tolerance = Math.Clamp(tolerance, 0, 255);
        var px = img.Pixels;
        uint target = px[y * w + x];
        if (!contiguous) return ByValue(img, target, tolerance);
        var stack = new Stack<(int, int)>();
        stack.Push((x, y));
        s.Set(x, y, true);
        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();
            Visit(cx - 1, cy); Visit(cx + 1, cy); Visit(cx, cy - 1); Visit(cx, cy + 1);
        }
        return s;

        void Visit(int i, int j)
        {
            if ((uint)i >= (uint)w || (uint)j >= (uint)h || s.Contains(i, j)) return;
            if (!Fill.Matches(px[j * w + i], target, tolerance)) return;
            s.Set(i, j, true);
            stack.Push((i, j));
        }
    }
}
