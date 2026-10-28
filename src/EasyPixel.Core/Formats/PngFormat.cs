using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Formats;

/// <summary>A PNG as a one-frame, one-layer RGBA sprite; saving writes the flattened first frame.</summary>
public sealed class PngFormat : ISpriteFormat
{
    public string Name => "PNG image";
    public string[] Extensions { get; } = { ".png" };
    public bool CanRead => true;
    public bool CanWrite => true;

    public Sprite Read(Stream s, List<string> warnings)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        var img = Png.Decode(ms.ToArray());
        var sp = new Sprite(img.Width, img.Height);
        sp.Cels[(sp.Layers[0], 0)] = new Cel(img);
        return sp;
    }

    public void Write(Sprite sp, Stream s, List<string> warnings)
    {
        if (sp.Frames.Count > 1) warnings.Add("Only the first frame was saved");
        var flat = new Image(sp.Width, sp.Height);
        Compositor.Flatten(sp, 0, flat, new PixelRect(0, 0, sp.Width, sp.Height));
        s.Write(Png.Encode(flat));
    }
}
