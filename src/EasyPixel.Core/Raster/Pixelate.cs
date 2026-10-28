namespace EasyPixel.Core.Raster;

public static class Pixelate
{
    /// <summary>
    /// Turns each <paramref name="cell"/> x <paramref name="cell"/> block into one flat value: the most common one in the
    /// block (first seen wins ties), so it works the same for RGBA and indexed images.
    /// </summary>
    public static void Apply(PixelSink s, int cell)
    {
        if (cell <= 1) return;
        var img = s.Image;
        int w = img.Width, h = img.Height;
        var src = (uint[])img.Pixels.Clone();
        var counts = new Dictionary<uint, int>();
        for (int by = 0; by < h; by += cell)
            for (int bx = 0; bx < w; bx += cell)
            {
                int ex = Math.Min(bx + cell, w), ey = Math.Min(by + cell, h);
                counts.Clear();
                uint best = src[by * w + bx];
                int bestN = 0;
                for (int y = by; y < ey; y++)
                    for (int x = bx; x < ex; x++)
                    {
                        uint v = src[y * w + x];
                        counts.TryGetValue(v, out int n);
                        counts[v] = ++n;
                        if (n > bestN) { bestN = n; best = v; }
                    }
                for (int y = by; y < ey; y++)
                    for (int x = bx; x < ex; x++) s.Set(x, y, best);
            }
    }
}
