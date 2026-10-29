using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Learn;

/// <summary>
/// A lesson's practice: a prepared document (a locked "Guide" layer with reference marks under the drawing layer,
/// optionally with something already drawn to work on) and the rules <see cref="Check"/> judges the drawing with.
/// The goal text and the self-check items live in the lesson file (they are translated with it).
/// </summary>
public sealed class Exercise
{
    /// <summary>
    /// The <see cref="Layer.Extra"/> key that marks the guide layer: <see cref="Check"/> leaves marked layers out of the
    /// drawing, whatever they are called (the person may rename them; .epx keeps the mark).
    /// </summary>
    public const string GuideKey = "easypixel:guide";
    /// <summary>
    /// The <see cref="Layer.Extra"/> key, on the guide layer, holding the exercise id (UTF-8): a saved practice document
    /// (.epx or .aseprite) knows which exercise it is, so opening it again brings the lesson bar back.
    /// </summary>
    public const string ExerciseKey = "easypixel:exercise";
    /// <summary>The default (English) layer names; the app passes them in the person's language.</summary>
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

    /// <summary>
    /// True for a guide layer of <paramref name="sprite"/>: one with the <see cref="GuideKey"/> mark. A document with
    /// no marked layer (an older practice, or one saved where the mark doesn't survive) falls back to
    /// layers named "Guide".
    /// </summary>
    public static bool IsGuide(Sprite sprite, Layer layer)
    {
        if (layer.IsGroup) return false;
        if (layer.Extra.ContainsKey(GuideKey)) return true;
        return layer.Name == GuideLayerName && !sprite.Layers.Any(l => l.Extra.ContainsKey(GuideKey));
    }

    /// <summary>
    /// A new practice document: the guide (locked, faint, marked with <see cref="GuideKey"/>) at the bottom, the drawing
    /// layer on top. The names are the person's to read (the app passes them translated); nothing depends on them.
    /// </summary>
    public Sprite CreateDocument(string guideName = GuideLayerName, string workName = WorkLayerName)
    {
        var s = new Sprite(Width, Height);
        s.Palette.Colors.AddRange(Palette);
        for (int i = 1; i < Frames; i++) s.Frames.Add(new Frame());
        var guide = s.Layers[0];
        guide.Name = guideName;
        guide.Extra[GuideKey] = new byte[] { 1 };
        guide.Extra[ExerciseKey] = System.Text.Encoding.UTF8.GetBytes(Id);
        guide.Locked = true;
        guide.Opacity = GuideOpacity;
        var g = new ArtCanvas(Width, Height);
        Guide?.Invoke(g);
        var guideCel = new Cel(g.Image.Clone()); // one picture, linked into every frame
        for (int f = 0; f < s.Frames.Count; f++) s.Cels[(guide, f)] = guideCel;

        var work = new Layer { Name = workName };
        s.Layers.Add(work);
        if (Start != null)
        {
            var w = new ArtCanvas(Width, Height);
            Start(w);
            w.CopyInto(s, work);
        }
        return s;
    }

    /// <summary>
    /// The exercise a practice document was made for (the <see cref="ExerciseKey"/> on its guide layer), or null for an
    /// ordinary document or an id no exercise has any more.
    /// </summary>
    public static Exercise? Of(Sprite sprite)
    {
        foreach (var l in sprite.Layers)
            if (!l.IsGroup && l.Extra.TryGetValue(ExerciseKey, out var b) && b.Length > 0)
            {
                var id = System.Text.Encoding.UTF8.GetString(b);
                return Exercises.Exists(id) ? Exercises.Get(id) : null;
            }
        return null;
    }

    /// <summary>
    /// Judges the drawing (every layer except the guide) with every rule. When every drawing layer is hidden, the one
    /// line says so instead (the rules would only see an empty picture).
    /// </summary>
    public CheckResult Check(Sprite sprite)
    {
        if (DrawingHidden(sprite)) return new CheckResult(new[] { HiddenDrawing.Line });
        var input = Input(sprite);
        return new(Rules.Select(r => r.Check(input)).ToList());
    }

    /// <summary>True when the document has drawing layers (not the guide) and none of them is shown.</summary>
    public static bool DrawingHidden(Sprite sprite)
    {
        var work = sprite.Layers.Where(l => !l.IsGroup && !IsGuide(sprite, l)).ToList();
        return work.Count > 0 && !work.Any(Shown);
    }

    /// <summary>The layer and every group above it are visible.</summary>
    private static bool Shown(Layer l)
    {
        for (var p = l; p != null; p = p.Parent) if (!p.Visible) return false;
        return true;
    }

    /// <summary>The drawing flattened per frame, and the guide flattened apart.</summary>
    public static CheckInput Input(Sprite sprite)
    {
        var visible = sprite.Layers.ToDictionary(l => l, l => l.Visible);
        try
        {
            foreach (var l in sprite.Layers) if (IsGuide(sprite, l)) l.Visible = false;
            var frames = new List<Image>();
            for (int f = 0; f < sprite.Frames.Count; f++) frames.Add(Flat(sprite, f));
            foreach (var l in sprite.Layers) l.Visible = IsGuide(sprite, l);
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
