namespace EasyPixel.Core.Raster;

public static class Fill
{
    /// <summary>
    /// Bucket fill. Contiguous: the 4-connected region around (x, y); otherwise every matching pixel of the image.
    /// A pixel matches when no channel differs from the start pixel by more than <paramref name="tolerance"/> (0..255).
    /// The sink's clip bounds the fill. Scanline with an explicit stack: no recursion.
    /// </summary>
    public static void Flood(PixelSink s, int x, int y, uint color, bool contiguous, int tolerance)
    {
        var img = s.Image;
        int w = img.Width, h = img.Height;
        if ((uint)x >= (uint)w || (uint)y >= (uint)h || !s.CanWrite(x, y)) return;
        var px = img.Pixels;
        uint target = px[y * w + x];
        tolerance = Math.Clamp(tolerance, 0, 255);
        if (target == color && tolerance == 0) return;

        if (!contiguous)
        {
            // Decide on the original values first: a written pixel may match again within the tolerance.
            var hit = new bool[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    hit[j * w + i] = Matches(px[j * w + i], target, tolerance) && s.CanWrite(i, j);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (hit[j * w + i]) s.Set(i, j, color);
            return;
        }

        var seen = new bool[w * h];
        bool clipped = s.HasClip;
        var stack = new Stack<(int, int)>();
        stack.Push((x, y));
        while (stack.Count > 0)
        {
            var (sx, sy) = stack.Pop();
            if (seen[sy * w + sx] || !Ok(sx, sy)) continue;
            int l = sx, r = sx;
            while (l > 0 && !seen[sy * w + l - 1] && Ok(l - 1, sy)) l--;
            while (r < w - 1 && !seen[sy * w + r + 1] && Ok(r + 1, sy)) r++;
            Array.Fill(seen, true, sy * w + l, r - l + 1);
            s.SetSpan(sy, l, r, color);
            Seed(sy - 1, l, r);
            Seed(sy + 1, l, r);
        }

        bool Ok(int i, int j) => Matches(px[j * w + i], target, tolerance) && (!clipped || s.CanWrite(i, j));

        // One seed per run of matching, unvisited pixels in the neighbouring row.
        void Seed(int j, int l, int r)
        {
            if (j < 0 || j >= h) return;
            bool inRun = false;
            for (int i = l; i <= r; i++)
            {
                bool ok = !seen[j * w + i] && Ok(i, j);
                if (ok && !inRun) stack.Push((i, j));
                inRun = ok;
            }
        }
    }

    internal static bool Matches(uint a, uint b, int tol)
    {
        if (a == b) return true;
        if (tol == 0) return false;
        return Math.Abs((int)(a & 255) - (int)(b & 255)) <= tol
            && Math.Abs((int)(a >> 8 & 255) - (int)(b >> 8 & 255)) <= tol
            && Math.Abs((int)(a >> 16 & 255) - (int)(b >> 16 & 255)) <= tol
            && Math.Abs((int)(a >> 24) - (int)(b >> 24)) <= tol;
    }
}
