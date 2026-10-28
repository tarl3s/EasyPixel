namespace EasyPixel.Core.Model;

public enum ColorMode { Rgba, Indexed }

public sealed class Palette
{
    public const int MaxColors = 256;

    public List<Rgba> Colors { get; } = new();

    /// <summary>Index drawn as transparent in indexed mode, or -1 when there is none.</summary>
    public int TransparentIndex { get; set; } = -1;

    /// <summary>Index of the exact colour, or -1.</summary>
    public int IndexOf(Rgba c) => Colors.IndexOf(c);

    /// <summary>Appends a colour and returns its index. Throws when the palette already holds <see cref="MaxColors"/>.</summary>
    public int Add(Rgba c)
    {
        if (Colors.Count >= MaxColors) throw new InvalidOperationException("Palette is full.");
        Colors.Add(c);
        return Colors.Count - 1;
    }

    /// <summary>Index of the closest entry by squared RGBA distance (ties: lowest index), skipping <paramref name="skip"/>; -1 if none.</summary>
    public int Nearest(Rgba c, int skip = -1)
    {
        int best = -1, bestD = int.MaxValue;
        for (int i = 0; i < Colors.Count; i++)
        {
            if (i == skip) continue;
            int d = Colors[i].DistanceSquared(c);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }
}
