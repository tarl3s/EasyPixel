using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

public static class Shading
{
    /// <summary>
    /// Aseprite-style shading: every pixel under the brush whose colour is in <paramref name="ramp"/> moves one step
    /// along it (lighten: next entry, darken: previous). Pixels not in the ramp, or at its end, are left alone.
    /// In indexed mode (<paramref name="indexed"/>) the pixel value is already the ramp index.
    /// </summary>
    public static void Apply(PixelSink s, int x, int y, Brush b, Palette ramp, bool lighten, bool indexed = false)
    {
        int step = lighten ? 1 : -1;
        Brush.ForEach(x, y, b, (px, py) =>
        {
            if (!s.CanWrite(px, py)) return;
            uint v = s.Get(px, py);
            int idx = indexed ? (v < (uint)ramp.Colors.Count ? (int)v : -1) : ramp.IndexOf(Rgba.FromPacked(v));
            int next = idx + step;
            if (idx < 0 || next < 0 || next >= ramp.Colors.Count) return;
            s.Set(px, py, indexed ? (uint)next : ramp.Colors[next].Packed);
        });
    }

    /// <summary>Changes the HSL lightness of each opaque RGBA pixel under the brush by <paramref name="amount"/> percent (negative darkens).</summary>
    public static void LightenDarken(PixelSink s, int x, int y, Brush b, int amount)
    {
        Brush.ForEach(x, y, b, (px, py) =>
        {
            if (!s.CanWrite(px, py)) return;
            var c = Rgba.FromPacked(s.Get(px, py));
            if (c.A == 0) return;
            RgbToHsl(c, out double h, out double sat, out double l);
            l = Math.Clamp(l + amount / 100.0, 0, 1);
            s.Set(px, py, HslToRgb(h, sat, l, c.A).Packed);
        });
    }

    private static void RgbToHsl(Rgba c, out double h, out double s, out double l)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        l = (max + min) / 2;
        double d = max - min;
        if (d < 1e-12) { h = 0; s = 0; return; }
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        if (max == r) h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
        else if (max == g) h = ((b - r) / d + 2) / 6;
        else h = ((r - g) / d + 4) / 6;
    }

    private static Rgba HslToRgb(double h, double s, double l, byte a)
    {
        double r, g, b;
        if (s < 1e-12) r = g = b = l;
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
            r = Hue(p, q, h + 1.0 / 3);
            g = Hue(p, q, h);
            b = Hue(p, q, h - 1.0 / 3);
        }
        return new Rgba(To8(r), To8(g), To8(b), a);
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 0.5) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    private static byte To8(double v) => (byte)Math.Clamp((int)Math.Round(v * 255), 0, 255);
}
