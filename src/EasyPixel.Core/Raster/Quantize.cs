using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

public static class Quantize
{
    /// <summary>Median-cut palette of the visible (alpha &gt; 0) colours of an RGBA image, at most <paramref name="maxColors"/> entries.</summary>
    public static Palette MedianCut(Image img, int maxColors)
    {
        maxColors = Math.Clamp(maxColors, 1, Palette.MaxColors);
        var counts = new Dictionary<uint, int>();
        foreach (uint p in img.Pixels)
        {
            if ((p >> 24) == 0) continue;
            counts.TryGetValue(p, out int n);
            counts[p] = n + 1;
        }
        var pal = new Palette();
        if (counts.Count == 0) return pal;

        var all = counts.Select(kv => (c: Rgba.FromPacked(kv.Key), n: kv.Value)).OrderBy(t => t.c.Packed).ToList();
        var boxes = new List<List<(Rgba c, int n)>> { all };
        while (boxes.Count < maxColors)
        {
            // Split the box with the widest channel range that still has two or more distinct colours.
            int bi = -1, bestRange = 0, bestCh = 0;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Count < 2) continue;
                for (int ch = 0; ch < 3; ch++)
                {
                    int lo = 255, hi = 0;
                    foreach (var (c, _) in boxes[i]) { int v = Channel(c, ch); lo = Math.Min(lo, v); hi = Math.Max(hi, v); }
                    if (hi - lo > bestRange) { bestRange = hi - lo; bi = i; bestCh = ch; }
                }
            }
            if (bi < 0) break;
            var box = boxes[bi].OrderBy(t => Channel(t.c, bestCh)).ThenBy(t => t.c.Packed).ToList();
            long total = box.Sum(t => (long)t.n), run = 0;
            int cut = 1;
            for (int i = 0; i < box.Count - 1; i++)
            {
                run += box[i].n;
                cut = i + 1;
                if (run * 2 >= total) break;
            }
            boxes[bi] = box.GetRange(0, cut);
            boxes.Add(box.GetRange(cut, box.Count - cut));
        }

        foreach (var box in boxes)
        {
            long n = box.Sum(t => (long)t.n), r = 0, g = 0, b = 0, a = 0;
            foreach (var (c, k) in box) { r += c.R * (long)k; g += c.G * (long)k; b += c.B * (long)k; a += c.A * (long)k; }
            pal.Colors.Add(new Rgba((byte)((r + n / 2) / n), (byte)((g + n / 2) / n), (byte)((b + n / 2) / n), (byte)((a + n / 2) / n)));
        }
        return pal;
    }

    /// <summary>
    /// Maps an RGBA image onto <paramref name="p"/>: each pixel becomes its nearest palette colour (Floyd-Steinberg
    /// error diffusion when <paramref name="dither"/>). Alpha 0 stays transparent. Returns a new RGBA image holding the
    /// palette colours; converting that to palette indices is Sprite.ConvertMode's job.
    /// </summary>
    public static Image ToPalette(Image img, Palette p, bool dither)
    {
        var res = img.Clone();
        if (p.Colors.Count == 0) return res;
        int w = img.Width, h = img.Height;
        var cur = new float[w * 3];
        var next = new float[w * 3];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                uint v = img.Pixels[y * w + x];
                if ((v >> 24) == 0) { res.Pixels[y * w + x] = 0; continue; }
                var c = Rgba.FromPacked(v);
                var want = c;
                if (dither)
                    want = new Rgba(Clamp(c.R + cur[x * 3]), Clamp(c.G + cur[x * 3 + 1]), Clamp(c.B + cur[x * 3 + 2]), c.A);
                var hit = p.Colors[p.Nearest(want)];
                res.Pixels[y * w + x] = hit.Packed;
                if (!dither) continue;
                float er = want.R - hit.R, eg = want.G - hit.G, eb = want.B - hit.B;
                Spread(cur, x + 1, 7 / 16f, er, eg, eb, w);
                Spread(next, x - 1, 3 / 16f, er, eg, eb, w);
                Spread(next, x, 5 / 16f, er, eg, eb, w);
                Spread(next, x + 1, 1 / 16f, er, eg, eb, w);
            }
            (cur, next) = (next, cur);
            Array.Clear(next);
        }
        return res;
    }

    private static void Spread(float[] row, int x, float k, float r, float g, float b, int w)
    {
        if ((uint)x >= (uint)w) return;
        row[x * 3] += r * k;
        row[x * 3 + 1] += g * k;
        row[x * 3 + 2] += b * k;
    }

    private static int Channel(Rgba c, int ch) => ch == 0 ? c.R : ch == 1 ? c.G : c.B;
    private static byte Clamp(float v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);
}
