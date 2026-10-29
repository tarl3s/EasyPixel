using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

public static class Shading
{
    /// <summary>
    /// Aseprite-style shading of one pixel: a value whose colour is in <paramref name="ramp"/> moves one step along it
    /// (lighten: next entry, darken: previous). Returns null when it stays: transparent, not in the ramp, or at its end.
    /// In indexed mode (<paramref name="indexed"/>) the value is already the ramp index and the ramp is the sprite's
    /// palette, whose <paramref name="transparentIndex"/> is never shaded from or into.
    /// </summary>
    public static uint? Step(uint v, Palette ramp, bool lighten, bool indexed = false, int transparentIndex = -1)
    {
        int step = lighten ? 1 : -1, count = ramp.Colors.Count;
        if (indexed)
        {
            if (v >= (uint)count || (int)v == transparentIndex) return null;
            int next = (int)v + step;
            return next < 0 || next >= count || next == transparentIndex ? null : (uint)next;
        }
        if ((v >> 24) == 0) return null;
        int idx = ramp.IndexOf(Rgba.FromPacked(v)), to = idx + step;
        if (idx < 0 || to < 0 || to >= count || ramp.Colors[to].A == 0) return null;
        return ramp.Colors[to].Packed;
    }

    /// <summary>Changes the HSL lightness of every opaque RGBA pixel under the brush by <paramref name="amount"/> percent (negative darkens).</summary>
    public static void LightenDarken(PixelSink s, int x, int y, Brush b, int amount)
    {
        Brush.ForEach(x, y, b, (px, py) =>
        {
            if (s.CanWrite(px, py) && Lighten(s.Get(px, py), amount) is uint v) s.Set(px, py, v);
        });
    }

    /// <summary>The RGBA value <paramref name="v"/> with its HSL lightness changed by <paramref name="amount"/> percent, or null when it is transparent.</summary>
    public static uint? Lighten(uint v, int amount)
    {
        var c = Rgba.FromPacked(v);
        if (c.A == 0) return null;
        RgbToHsl(c, out double h, out double sat, out double l);
        l = Math.Clamp(l + amount / 100.0, 0, 1);
        return HslToRgb(h, sat, l, c.A).Packed;
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
