namespace EasyPixel.Core.Raster;

public enum DitherKind { Checker, Bayer2, Bayer4, Bayer8 }

/// <summary>Ordered-dither threshold matrices.</summary>
public static class Dither
{
    private static readonly int[,] B2 = { { 0, 2 }, { 3, 1 } };
    private static readonly int[,] B4 = BuildBayer(4);
    private static readonly int[,] B8 = BuildBayer(8);

    /// <summary>Matrix side length.</summary>
    public static int Size(DitherKind k) => k switch { DitherKind.Bayer4 => 4, DitherKind.Bayer8 => 8, _ => 2 };

    /// <summary>Threshold at (x, y), 0 .. cells-1 (Checker: 0 or 1).</summary>
    public static int Threshold(DitherKind k, int x, int y)
    {
        int n = Size(k);
        int i = ((x % n) + n) % n, j = ((y % n) + n) % n;
        return k switch
        {
            DitherKind.Checker => (i + j) & 1,
            DitherKind.Bayer2 => B2[j, i],
            DitherKind.Bayer4 => B4[j, i],
            _ => B8[j, i],
        };
    }

    /// <summary>True when the pixel is painted at the given coverage (0..1, default half).</summary>
    public static bool Pattern(DitherKind kind, int x, int y, double density = 0.5)
    {
        int cells = kind == DitherKind.Checker ? 2 : Size(kind) * Size(kind);
        return (Threshold(kind, x, y) + 0.5) / cells < density;
    }

    private static int[,] BuildBayer(int n)
    {
        var m = new int[n, n];
        for (int s = 1; s < n; s *= 2)
        {
            var prev = (int[,])m.Clone();
            for (int j = 0; j < s; j++)
                for (int i = 0; i < s; i++)
                {
                    int v = prev[j, i] * 4;
                    m[j, i] = v;
                    m[j, i + s] = v + 2;
                    m[j + s, i] = v + 3;
                    m[j + s, i + s] = v + 1;
                }
        }
        return m;
    }
}
