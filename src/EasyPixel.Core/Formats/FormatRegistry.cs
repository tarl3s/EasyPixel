namespace EasyPixel.Core.Formats;

/// <summary>All known formats; later stages add theirs to <see cref="All"/>.</summary>
public static class FormatRegistry
{
    public static List<ISpriteFormat> All { get; } = new() { new EpxFormat(), new PngFormat(), new AsepriteFormat(), new PiskelFormat(), new GifFormat() };

    /// <summary>The format whose extension matches the path (case-insensitive), or null.</summary>
    public static ISpriteFormat? ForPath(string path)
    {
        string ext = Path.GetExtension(path);
        return All.FirstOrDefault(f => f.Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase));
    }
}
