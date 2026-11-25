using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

/// <summary>The brush effects that change the pixels already there: jumble (swap with a neighbour) and blur.</summary>
public static class PixelEffects
{
    private static readonly (int Dx, int Dy)[] Neighbours =
        { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1) };

    /// <summary>
    /// Jumble (LibreSprite's tool): with <paramref name="percent"/>% chance, swaps the pixel at (x, y) with a random
    /// neighbour it may write. Pixels only move, so no colour is added or lost.
    /// </summary>
    public static void JumbleAt(PixelSink sink, int x, int y, int percent, Random random)
    {
        if (!sink.CanWrite(x, y) || random.Next(100) >= percent) return;
        var (dx, dy) = Neighbours[random.Next(Neighbours.Length)];
        int nx = x + dx, ny = y + dy;
        if (!sink.CanWrite(nx, ny)) return;
        uint a = sink.Get(x, y), b = sink.Get(nx, ny);
        if (a == b) return;
        sink.Set(x, y, b);
        sink.Set(nx, ny, a);
    }

    /// <summary>
    /// Blur: the pixel at (x, y) moves <paramref name="percent"/>% of the way to the average of its 3x3 neighbourhood,
    /// read through <paramref name="read"/> (packed RGBA). Colours are weighted by their opacity, so transparent
    /// neighbours fade the edge instead of turning it black. Returns the new colour.
    /// </summary>
    public static Rgba BlurAt(Func<int, int, Rgba?> read, int x, int y, int percent)
    {
        var self = read(x, y) ?? Rgba.Transparent;
        long r = 0, g = 0, b = 0, a = 0;
        int n = 0;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (read(x + dx, y + dy) is not { } c) continue;
                r += c.R * c.A; g += c.G * c.A; b += c.B * c.A; a += c.A;
                n++;
            }
        if (n == 0) return self;
        Rgba avg = a == 0 ? Rgba.Transparent : new((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n));
        int p = Math.Clamp(percent, 0, 100);
        byte Mix(byte from, byte to) => (byte)((from * (100 - p) + to * p + 50) / 100);
        // a fully transparent pixel has no colour of its own: it takes the average's colour, only its opacity mixes
        return self.A == 0
            ? new Rgba(avg.R, avg.G, avg.B, Mix(0, avg.A))
            : new Rgba(Mix(self.R, avg.R), Mix(self.G, avg.G), Mix(self.B, avg.B), Mix(self.A, avg.A));
    }
}
