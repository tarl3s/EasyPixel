using EasyPixel.Core.Model;

namespace EasyPixel.Core.Render;

/// <summary>
/// Pixel blending with the semantics of Aseprite's doc/blend_funcs.cpp: the blend function runs per channel on
/// straight (unpremultiplied) colour, then the result is composited source-over with the combined alpha.
/// All arithmetic is 8-bit integer (MUL_UN8 / DIV_UN8 rounding), so results are reproducible.
/// </summary>
public static class BlendModes
{
    /// <summary>Blends <paramref name="front"/> over <paramref name="back"/> (both <see cref="Rgba.Packed"/>) at the given opacity.</summary>
    public static uint Blend(BlendMode m, uint back, uint front, byte opacity)
    {
        int ba = (int)(back >> 24), sa = (int)(front >> 24);
        if (sa == 0 || opacity == 0) return back;
        sa = Mul(sa, opacity);
        if (sa == 0) return back;
        if (ba == 0) return (front & 0x00FFFFFFu) | ((uint)sa << 24);

        int br = (int)(back & 0xFF), bg = (int)((back >> 8) & 0xFF), bb = (int)((back >> 16) & 0xFF);
        int sr = (int)(front & 0xFF), sg = (int)((front >> 8) & 0xFF), sb = (int)((front >> 16) & 0xFF);
        int ra = sa + ba - Mul(ba, sa);

        if (m != BlendMode.Normal)
        {
            int tr, tg, tb;
            if (m >= BlendMode.Hue && m <= BlendMode.Luminosity)
                NonSeparable(m, br, bg, bb, sr, sg, sb, out tr, out tg, out tb);
            else
            {
                tr = Channel(m, br, sr); tg = Channel(m, bg, sg); tb = Channel(m, bb, sb);
            }
            // Where the backdrop is see-through the source shows as itself.
            sr += (tr - sr) * ba / 255; sg += (tg - sg) * ba / 255; sb += (tb - sb) * ba / 255;
        }

        int rr = br + (sr - br) * sa / ra;
        int rg = bg + (sg - bg) * sa / ra;
        int rb = bb + (sb - bb) * sa / ra;
        return (uint)(rr | rg << 8 | rb << 16 | ra << 24);
    }

    private static int Mul(int a, int b) { int t = a * b + 0x80; return ((t >> 8) + t) >> 8; }
    private static int Div(int a, int b) => (a * 255 + b / 2) / b;

    private static int Channel(BlendMode m, int b, int s)
    {
        switch (m)
        {
            case BlendMode.Multiply: return Mul(b, s);
            case BlendMode.Screen: return b + s - Mul(b, s);
            case BlendMode.Overlay: return HardLight(s, b);
            case BlendMode.Darken: return Math.Min(b, s);
            case BlendMode.Lighten: return Math.Max(b, s);
            case BlendMode.ColorDodge:
                if (b == 0) return 0;
                s = 255 - s;
                return b >= s ? 255 : Div(b, s);
            case BlendMode.ColorBurn:
                if (b == 255) return 255;
                b = 255 - b;
                return b >= s ? 0 : 255 - Div(b, s);
            case BlendMode.HardLight: return HardLight(b, s);
            case BlendMode.SoftLight: return SoftLight(b, s);
            case BlendMode.Difference: return Math.Abs(b - s);
            case BlendMode.Exclusion: return b + s - 2 * Mul(b, s);
            case BlendMode.Addition: return Math.Min(255, b + s);
            case BlendMode.Subtract: return Math.Max(0, b - s);
            case BlendMode.Divide:
                if (b == 0) return 0;
                return b >= s ? 255 : Div(b, s);
            default: return s;
        }
    }

    private static int HardLight(int b, int s)
    {
        if (s < 128) return Mul(b, s << 1);
        s = (s << 1) - 255;
        return b + s - Mul(b, s);
    }

    private static int SoftLight(int b8, int s8)
    {
        double b = b8 / 255.0, s = s8 / 255.0;
        double d = b <= 0.25 ? ((16 * b - 12) * b + 4) * b : Math.Sqrt(b);
        double r = s <= 0.5 ? b - (1 - 2 * s) * b * (1 - b) : b + (2 * s - 1) * (d - b);
        return (int)(r * 255 + 0.5);
    }

    // ---- non-separable modes (W3C compositing spec, 8-bit) ----

    private static int Lum(int r, int g, int b) => (r * 30 + g * 59 + b * 11) / 100;
    private static int Sat(int r, int g, int b) => Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));

    private static void NonSeparable(BlendMode m, int br, int bg, int bb, int sr, int sg, int sb, out int r, out int g, out int b)
    {
        switch (m)
        {
            case BlendMode.Hue:
                r = sr; g = sg; b = sb;
                SetSat(ref r, ref g, ref b, Sat(br, bg, bb));
                SetLum(ref r, ref g, ref b, Lum(br, bg, bb));
                break;
            case BlendMode.Saturation:
                r = br; g = bg; b = bb;
                SetSat(ref r, ref g, ref b, Sat(sr, sg, sb));
                SetLum(ref r, ref g, ref b, Lum(br, bg, bb));
                break;
            case BlendMode.Color:
                r = sr; g = sg; b = sb;
                SetLum(ref r, ref g, ref b, Lum(br, bg, bb));
                break;
            default: // Luminosity
                r = br; g = bg; b = bb;
                SetLum(ref r, ref g, ref b, Lum(sr, sg, sb));
                break;
        }
    }

    private static void SetLum(ref int r, ref int g, ref int b, int l)
    {
        int d = l - Lum(r, g, b);
        r += d; g += d; b += d;
        int lum = Lum(r, g, b), n = Math.Min(r, Math.Min(g, b)), x = Math.Max(r, Math.Max(g, b));
        if (n < 0)
        {
            r = lum + (r - lum) * lum / (lum - n);
            g = lum + (g - lum) * lum / (lum - n);
            b = lum + (b - lum) * lum / (lum - n);
        }
        if (x > 255)
        {
            r = lum + (r - lum) * (255 - lum) / (x - lum);
            g = lum + (g - lum) * (255 - lum) / (x - lum);
            b = lum + (b - lum) * (255 - lum) / (x - lum);
        }
    }

    private static void SetSat(ref int r, ref int g, ref int b, int s)
    {
        int min = Math.Min(r, Math.Min(g, b)), max = Math.Max(r, Math.Max(g, b));
        if (max == min) { r = g = b = 0; return; }
        r = (r - min) * s / (max - min);
        g = (g - min) * s / (max - min);
        b = (b - min) * s / (max - min);
    }
}
