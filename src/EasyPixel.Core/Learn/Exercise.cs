using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Learn;

/// <summary>
/// A lesson's practice: a prepared document (a locked "Guide" layer with reference marks under a "Drawing" layer,
/// optionally with something already drawn to work on) and the rules <see cref="Check"/> judges the drawing with.
/// The goal text and the self-check items live in the lesson file (they are translated with it).
/// </summary>
public sealed class Exercise
{
    /// <summary>The guide layer's name: <see cref="Check"/> leaves layers with this name out of the drawing.</summary>
    public const string GuideLayerName = "Guide";
    public const string WorkLayerName = "Drawing";

    public required string Id { get; init; }
    public int Width { get; init; } = 32;
    public int Height { get; init; } = 32;
    /// <summary>Frames the document starts with (an animation exercise may start with more than one).</summary>
    public int Frames { get; init; } = 1;
    /// <summary>Draws the reference marks (shown faintly, locked). Null = an empty guide layer.</summary>
    public Action<ArtCanvas>? Guide { get; init; }
    /// <summary>Draws what the drawing layer starts with (e.g. a shape to outline). Null = empty.</summary>
    public Action<ArtCanvas>? Start { get; init; }
    public required IReadOnlyList<CheckRule> Rules { get; init; }
    /// <summary>The colours offered in the palette panel.</summary>
    public IReadOnlyList<Rgba> Palette { get; init; } = Pal.All;
    /// <summary>The guide layer's opacity (0–255).</summary>
    public byte GuideOpacity { get; init; } = 128;

    public static bool IsGuide(Layer layer) => !layer.IsGroup && layer.Name == GuideLayerName;

    /// <summary>A new practice document: the guide (locked, faint) at the bottom, the drawing layer on top.</summary>
    public Sprite CreateDocument()
    {
        var s = new Sprite(Width, Height);
        s.Palette.Colors.AddRange(Palette);
        for (int i = 1; i < Frames; i++) s.Frames.Add(new Frame());
        var guide = s.Layers[0];
        guide.Name = GuideLayerName;
        guide.Locked = true;
        guide.Opacity = GuideOpacity;
        var g = new ArtCanvas(Width, Height);
        Guide?.Invoke(g);
        var guideCel = new Cel(g.Image.Clone()); // one picture, linked into every frame
        for (int f = 0; f < s.Frames.Count; f++) s.Cels[(guide, f)] = guideCel;

        var work = new Layer { Name = WorkLayerName };
        s.Layers.Add(work);
        if (Start != null)
        {
            var w = new ArtCanvas(Width, Height);
            Start(w);
            w.CopyInto(s, work);
        }
        return s;
    }

    /// <summary>Judges the drawing (every layer except the guide) with every rule.</summary>
    public CheckResult Check(Sprite sprite) => new(Rules.Select(r => r.Check(Input(sprite))).ToList());

    /// <summary>The drawing flattened per frame, and the guide flattened apart.</summary>
    public static CheckInput Input(Sprite sprite)
    {
        var visible = sprite.Layers.ToDictionary(l => l, l => l.Visible);
        try
        {
            foreach (var l in sprite.Layers) if (IsGuide(l)) l.Visible = false;
            var frames = new List<Image>();
            for (int f = 0; f < sprite.Frames.Count; f++) frames.Add(Flat(sprite, f));
            foreach (var l in sprite.Layers) l.Visible = IsGuide(l);
            var guide = Flat(sprite, 0);
            return new CheckInput(sprite, frames, guide);
        }
        finally
        {
            foreach (var (l, v) in visible) l.Visible = v;
        }
    }

    private static Image Flat(Sprite s, int frame)
    {
        var img = new Image(s.Width, s.Height);
        Compositor.Flatten(s, frame, img, new PixelRect(0, 0, s.Width, s.Height), scratch: new CompositeScratch());
        return img;
    }
}
