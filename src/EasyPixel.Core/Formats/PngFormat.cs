using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Formats;

/// <summary>A PNG as a one-frame, one-layer RGBA sprite; saving writes the flattened first frame.</summary>
public sealed class PngFormat : ISpriteFormat
{
    public const string FirstFrameOnlyWarning = "Only the first frame was saved";

    public string Name => "PNG image";
    public string[] Extensions { get; } = { ".png" };
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool Flattens => true;

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
        if (sp.Frames.Count > 1) warnings.Add(FirstFrameOnlyWarning);
        var flat = new Image(sp.Width, sp.Height);
        Compositor.Flatten(sp, 0, flat, new PixelRect(0, 0, sp.Width, sp.Height));
        s.Write(Png.Encode(flat));
    }
}

/// <summary>What a flattened picture (PNG, GIF) can't keep of a sprite, as warnings for the person (shown when saving to one).</summary>
public static class FlatPicture
{
    public const string LayersMergedWarning = "The layers were merged into one picture";
    public const string HiddenLayersWarning = "Hidden layers were left out";
    public const string TagsWarning = "Tags were not saved";
    public const string PaletteWarning = "The colour palette was not saved (indexed colours became plain colours)";

    public static void Warn(Sprite sp, List<string> warnings)
    {
        if (sp.Layers.Count(l => !l.IsGroup) > 1) warnings.Add(LayersMergedWarning);
        if (sp.Layers.Any(l => !l.Visible)) warnings.Add(HiddenLayersWarning);
        if (sp.Tags.Count > 0) warnings.Add(TagsWarning);
        // An RGBA sprite's palette is only a list of swatches to pick from; an indexed one's numbers become plain colours.
        if (sp.Mode == ColorMode.Indexed) warnings.Add(PaletteWarning);
    }
}
