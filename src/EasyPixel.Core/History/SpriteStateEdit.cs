using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>The whole picture at one moment: canvas size, colour mode, palette and every cel (image, position).</summary>
public sealed class SpriteState
{
    internal int Width, Height;
    internal ColorMode Mode;
    internal List<Rgba> Colors = new();
    internal int TransparentIndex;
    internal List<KeyValuePair<(Layer, int), Cel>> Keys = new();
    internal List<(Cel Cel, Image Image, int X, int Y)> Cels = new();

    /// <summary>
    /// Copies the sprite. With <paramref name="cloneImages"/> the images are private copies (a "before" state, the
    /// change may draw into them); without, the live images are kept (an "after" state).
    /// </summary>
    internal static SpriteState Take(Sprite s, bool cloneImages) => new()
    {
        Width = s.Width, Height = s.Height, Mode = s.Mode,
        Colors = s.Palette.Colors.ToList(), TransparentIndex = s.Palette.TransparentIndex,
        Keys = s.Cels.ToList(),
        Cels = s.Cels.Values.Distinct().Select(c => (c, cloneImages ? c.Image.Clone() : c.Image, c.X, c.Y)).ToList(),
    };

    internal void Restore(Sprite s)
    {
        s.SetCanvasSize(Width, Height);
        s.SetMode(Mode);
        s.Palette.Colors.Clear();
        s.Palette.Colors.AddRange(Colors);
        s.Palette.TransparentIndex = TransparentIndex;
        s.Cels.Clear();
        foreach (var kv in Keys) s.Cels[kv.Key] = kv.Value;
        foreach (var (cel, img, x, y) in Cels) { cel.Image = img; cel.X = x; cel.Y = y; }
    }

    internal long Bytes => Cels.Sum(c => c.Image.Pixels.Length * 4L) + Keys.Count * 24L + Colors.Count * 4L;
}

/// <summary>
/// A change to the whole sprite (rotate, flip, scale, colour mode, reduce colours, palette changes that renumber pixels):
/// every cel's image before and after. Costs two copies of the picture; use smaller edits for smaller changes.
/// </summary>
public sealed class SpriteStateEdit : IEdit
{
    private readonly SpriteState _before, _after;

    private SpriteStateEdit(string name, SpriteState before, SpriteState after) { Name = name; _before = before; _after = after; }

    public string Name { get; }
    public long Bytes => 64 + _before.Bytes + _after.Bytes;

    /// <summary>Copies the sprite; call it before changing it.</summary>
    public static SpriteState Begin(Sprite s) => SpriteState.Take(s, cloneImages: true);

    /// <summary>Records the change from <paramref name="before"/> to the sprite as it is now.</summary>
    public static SpriteStateEdit Capture(Sprite s, SpriteState before, string name) => new(name, before, SpriteState.Take(s, cloneImages: false));

    /// <summary>Runs <paramref name="change"/> on the sprite and records it.</summary>
    public static SpriteStateEdit Perform(Sprite s, string name, Action change)
    {
        var before = Begin(s);
        change();
        return Capture(s, before, name);
    }

    public void Undo(Sprite s) => _before.Restore(s);
    public void Redo(Sprite s) => _after.Restore(s);
}

/// <summary>A palette change that leaves the pixels alone (add or edit an entry; any change on an RGBA sprite).</summary>
public sealed class PaletteEdit : IEdit
{
    private readonly List<Rgba> _before, _after;
    private readonly int _tBefore, _tAfter;

    private PaletteEdit(string name, List<Rgba> before, int tBefore, List<Rgba> after, int tAfter)
    {
        Name = name; _before = before; _tBefore = tBefore; _after = after; _tAfter = tAfter;
    }

    public string Name { get; }
    public long Bytes => 64 + (_before.Count + _after.Count) * 4L;

    public static PaletteEdit Perform(Sprite s, string name, Action change)
    {
        var before = s.Palette.Colors.ToList();
        int t = s.Palette.TransparentIndex;
        change();
        return new PaletteEdit(name, before, t, s.Palette.Colors.ToList(), s.Palette.TransparentIndex);
    }

    public void Undo(Sprite s) => Set(s, _before, _tBefore);
    public void Redo(Sprite s) => Set(s, _after, _tAfter);

    private static void Set(Sprite s, List<Rgba> colors, int t)
    {
        s.Palette.Colors.Clear();
        s.Palette.Colors.AddRange(colors);
        s.Palette.TransparentIndex = t;
    }
}
