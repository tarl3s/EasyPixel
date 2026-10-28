using Avalonia.Media;

namespace EasyPixel;

/// <summary>WCAG contrast between two colours (1 to 21; body text needs at least 4.5).</summary>
public static class Contrast
{
    public static double Ratio(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>Relative luminance of an opaque colour (sRGB).</summary>
    public static double Luminance(Color c)
    {
        static double Ch(byte v)
        {
            double s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Ch(c.R) + 0.7152 * Ch(c.G) + 0.0722 * Ch(c.B);
    }

    /// <summary><paramref name="top"/> (with its alpha, times <paramref name="opacity"/>) over an opaque <paramref name="under"/>.</summary>
    public static Color Over(Color top, double opacity, Color under)
    {
        double a = top.A / 255.0 * opacity;
        byte Mix(byte t, byte u) => (byte)Math.Round(t * a + u * (1 - a));
        return Color.FromRgb(Mix(top.R, under.R), Mix(top.G, under.G), Mix(top.B, under.B));
    }
}
