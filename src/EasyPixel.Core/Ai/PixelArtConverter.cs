using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Ai;

/// <summary>How to turn a big generated picture into a sprite: its size, colour limit, optional fixed palette and outline.</summary>
public sealed record ConvertOptions(int Size, int MaxColors = 12, IReadOnlyList<Rgba>? Palette = null, bool Outline = true);

/// <summary>Turns a big AI-generated picture into a clean small pixel-art sprite. Each step is public so it can be tested and tuned.</summary>
public static class PixelArtConverter
{
    private const int BackgroundTolerance = 40;

    /// <summary>The whole pipeline. Null when the picture has no subject (nothing but background).</summary>
    public static Image? Convert(Image big, ConvertOptions o)
    {
        var cut = RemoveBackground(big);
        var box = SubjectBounds(cut);
        if (box.W <= 0 || box.H <= 0) return null;
        var small = Downscale(cut, box, o.Size);
        small = LimitColors(small, o.MaxColors, o.Palette);
        Cleanup(small);
        if (small.Pixels.All(p => p == 0)) return null;
        if (o.Outline) AddOutline(small);
        return small;
    }

    /// <summary>Makes the background transparent: the border's dominant colour is flooded in from the border, then only the main blob (and blobs of at least 2% of it) stays.</summary>
    public static Image RemoveBackground(Image src)
    {
        int w = src.Width, h = src.Height;
        var res = src.Clone();

        // Dominant colour of the 1-px border (grouped in 16-level steps so noise does not split it).
        var groups = new Dictionary<int, (int n, long r, long g, long b)>();
        void Vote(int x, int y)
        {
            var c = Rgba.FromPacked(src.Pixels[y * w + x]);
            if (c.A < 128) return;
            int key = (c.R >> 4) << 8 | (c.G >> 4) << 4 | (c.B >> 4);
            groups.TryGetValue(key, out var t);
            groups[key] = (t.n + 1, t.r + c.R, t.g + c.G, t.b + c.B);
        }
        for (int x = 0; x < w; x++) { Vote(x, 0); Vote(x, h - 1); }
        for (int y = 1; y < h - 1; y++) { Vote(0, y); Vote(w - 1, y); }

        if (groups.Count > 0)
        {
            var best = groups.Values.MaxBy(t => t.n);
            int br = (int)(best.r / best.n), bg = (int)(best.g / best.n), bb = (int)(best.b / best.n);
            bool IsBg(uint p)
            {
                var c = Rgba.FromPacked(p);
                if (c.A < 128) return true;
                int dr = c.R - br, dg = c.G - bg, db = c.B - bb;
                return dr * dr + dg * dg + db * db <= BackgroundTolerance * BackgroundTolerance;
            }
            var stack = new Stack<int>();
            void Seed(int x, int y)
            {
                int i = y * w + x;
                if (res.Pixels[i] != 0 && IsBg(res.Pixels[i])) { res.Pixels[i] = 0; stack.Push(i); }
            }
            for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
            for (int y = 0; y < h; y++) { Seed(0, y); Seed(w - 1, y); }
            while (stack.Count > 0)
            {
                int i = stack.Pop(), x = i % w, y = i / w;
                if (x > 0) Seed(x - 1, y);
                if (x < w - 1) Seed(x + 1, y);
                if (y > 0) Seed(x, y - 1);
                if (y < h - 1) Seed(x, y + 1);
            }
        }
        KeepMainBlobs(res);
        return res;
    }

    /// <summary>Clears every 8-connected blob smaller than 2% of the largest one (the largest always stays).</summary>
    private static void KeepMainBlobs(Image img)
    {
        int w = img.Width, h = img.Height;
        var label = new int[w * h];
        var sizes = new List<int> { 0 };
        var stack = new Stack<int>();
        for (int s = 0; s < label.Length; s++)
        {
            if (label[s] != 0 || (img.Pixels[s] >> 24) == 0) continue;
            int id = sizes.Count, n = 0;
            sizes.Add(0);
            label[s] = id;
            stack.Push(s);
            while (stack.Count > 0)
            {
                int i = stack.Pop(), x = i % w, y = i / w;
                n++;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
                        int j = ny * w + nx;
                        if (label[j] != 0 || (img.Pixels[j] >> 24) == 0) continue;
                        label[j] = id;
                        stack.Push(j);
                    }
            }
            sizes[id] = n;
        }
        int largest = sizes.Max();
        for (int i = 0; i < label.Length; i++)
            if (label[i] != 0 && sizes[label[i]] * 50 < largest) img.Pixels[i] = 0;
    }

    /// <summary>The box around the visible pixels; width 0 when there are none.</summary>
    public static PixelRect SubjectBounds(Image img)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if ((img.Pixels[y * img.Width + x] >> 24) == 0) continue;
                x0 = Math.Min(x0, x); x1 = Math.Max(x1, x);
                y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
            }
        return x1 < 0 ? new PixelRect(0, 0, 0, 0) : new PixelRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    /// <summary>
    /// Shrinks the <paramref name="from"/> box into a size x size image, keeping proportions, centred sideways, feet on
    /// row size - 2 (1-px margin all round). Each cell takes the most frequent opaque colour of its source area, never an average.
    /// </summary>
    public static Image Downscale(Image src, PixelRect from, int size)
    {
        var res = new Image(size, size);
        int room = Math.Max(1, size - 2);
        double scale = Math.Max((double)from.W / room, (double)from.H / room); // source pixels per target pixel
        int tw = Math.Min(room, Math.Max(1, (int)Math.Round(from.W / scale)));
        int th = Math.Min(room, Math.Max(1, (int)Math.Round(from.H / scale)));
        int ox = (size - tw) / 2, oy = size - 1 - th;

        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                int sx0 = (int)Math.Floor(tx * (double)from.W / tw), sy0 = (int)Math.Floor(ty * (double)from.H / th);
                int sx1 = Math.Max((int)Math.Floor((tx + 1) * (double)from.W / tw), sx0 + 1);
                int sy1 = Math.Max((int)Math.Floor((ty + 1) * (double)from.H / th), sy0 + 1);
                res.Set(ox + tx, oy + ty, PickColour(src, from.X + sx0, from.Y + sy0, Math.Min(from.X + sx1, src.Width), Math.Min(from.Y + sy1, src.Height)));
            }
        return res;
    }

    /// <summary>The colour a cell should show: its most frequent opaque colour (a much darker one covering a quarter wins, so eyes survive); 0 when under 40% is opaque.</summary>
    private static uint PickColour(Image src, int x0, int y0, int x1, int y1)
    {
        int total = Math.Max(1, (x1 - x0) * (y1 - y0)), opaque = 0;
        var groups = new Dictionary<int, (int n, long r, long g, long b)>();
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var c = Rgba.FromPacked(src.Pixels[y * src.Width + x]);
                if (c.A < 128) continue;
                opaque++;
                int key = (c.R >> 4) << 8 | (c.G >> 4) << 4 | (c.B >> 4);
                groups.TryGetValue(key, out var t);
                groups[key] = (t.n + 1, t.r + c.R, t.g + c.G, t.b + c.B);
            }
        if (opaque * 100 < total * 40) return 0;
        var top = groups.Values.MaxBy(t => t.n);
        var pick = new Rgba((byte)(top.r / top.n), (byte)(top.g / top.n), (byte)(top.b / top.n), 255);
        int pickLuma = Luma(pick);
        foreach (var t in groups.Values)
        {
            if (t.n * 4 < total) continue;
            var c = new Rgba((byte)(t.r / t.n), (byte)(t.g / t.n), (byte)(t.b / t.n), 255);
            if (Luma(c) + 80 < pickLuma) { pick = c; pickLuma = Luma(c); }
        }
        return pick.Packed;
    }

    private static int Luma(Rgba c) => (c.R * 3 + c.G * 6 + c.B) / 10;

    /// <summary>Median cut to <paramref name="max"/> colours, or the nearest of a fixed palette. No dithering.</summary>
    public static Image LimitColors(Image img, int max, IReadOnlyList<Rgba>? palette)
    {
        Palette pal;
        if (palette is { Count: > 0 })
        {
            pal = new Palette();
            foreach (var c in palette) pal.Colors.Add(c with { A = 255 });
        }
        else pal = Quantize.MedianCut(img, max);
        return Quantize.ToPalette(img, pal, false);
    }

    /// <summary>Makes alpha 0 or 255 and removes opaque pixels with no opaque 4-neighbour.</summary>
    public static void Cleanup(Image img)
    {
        for (int i = 0; i < img.Pixels.Length; i++)
        {
            var c = Rgba.FromPacked(img.Pixels[i]);
            img.Pixels[i] = c.A >= 128 ? (c with { A = 255 }).Packed : 0;
        }
        var src = img.Clone();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (src.Get(x, y) != 0 && Opaque(src, x - 1, y) + Opaque(src, x + 1, y) + Opaque(src, x, y - 1) + Opaque(src, x, y + 1) == 0)
                    img.Set(x, y, 0);
    }

    private static int Opaque(Image img, int x, int y) => (img.Get(x, y) >> 24) != 0 ? 1 : 0;

    /// <summary>Adds a 1-px ring outside the shape; each ring pixel is the darkest neighbouring colour, darkened by half.</summary>
    public static void AddOutline(Image img)
    {
        var src = img.Clone();
        int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (src.Get(x, y) != 0) continue;
                Rgba? darkest = null;
                for (int k = 0; k < 4; k++)
                {
                    uint p = src.Get(x + dx[k], y + dy[k]);
                    if ((p >> 24) == 0) continue;
                    var c = Rgba.FromPacked(p);
                    if (darkest is null || Luma(c) < Luma(darkest.Value)) darkest = c;
                }
                if (darkest is { } d) img.Set(x, y, new Rgba((byte)(d.R / 2), (byte)(d.G / 2), (byte)(d.B / 2), 255).Packed);
            }
    }
}
