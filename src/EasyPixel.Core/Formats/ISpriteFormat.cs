using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>A file format the editor can open and/or save. Readers throw <see cref="FormatException"/> on bad input and add notes about anything lost to <c>warnings</c>.</summary>
public interface ISpriteFormat
{
    string Name { get; }
    /// <summary>Lower-case extensions including the dot, e.g. ".epx".</summary>
    string[] Extensions { get; }
    bool CanRead { get; }
    bool CanWrite { get; }
    /// <summary>True for picture formats (PNG, GIF) that keep only the flattened image: saving to one is an export.</summary>
    bool Flattens => false;
    Sprite Read(Stream s, List<string> warnings);
    void Write(Sprite sp, Stream s, List<string> warnings);
}
