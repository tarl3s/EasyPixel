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

    internal static PaletteEdit Create(string name, List<Rgba> before, int tBefore, Sprite s) =>
        new(name, before, tBefore, s.Palette.Colors.ToList(), s.Palette.TransparentIndex);

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

/// <summary>The palette at one moment (colours and transparent index), to tell whether a gesture changed it.</summary>
public sealed class PaletteSnapshot
{
    private readonly Rgba[] _colors;
    private readonly int _transparent;

    private PaletteSnapshot(Rgba[] colors, int transparent) { _colors = colors; _transparent = transparent; }

    public static PaletteSnapshot Take(Sprite s) => new(s.Palette.Colors.ToArray(), s.Palette.TransparentIndex);

    public bool Matches(Sprite s) => s.Palette.TransparentIndex == _transparent && s.Palette.Colors.SequenceEqual(_colors);

    /// <summary>Puts the palette back as it was (for a gesture that is abandoned).</summary>
    public void Restore(Sprite s)
    {
        s.Palette.Colors.Clear();
        s.Palette.Colors.AddRange(_colors);
        s.Palette.TransparentIndex = _transparent;
    }

    /// <summary>The palette change from this snapshot to the sprite's palette now, or null when nothing changed.</summary>
    public PaletteEdit? EditSince(Sprite s, string name) =>
        Matches(s) ? null : PaletteEdit.Create(name, _colors.ToList(), _transparent, s);
}

/// <summary>
/// Several edits recorded as one undo step, in the order they were made (undo runs them backwards). Used when a gesture
/// also grew the palette of an indexed sprite, so undo removes the new entries together with the pixels.
/// </summary>
public sealed class CompoundEdit : IEdit
{
    private readonly IEdit[] _parts;

    public CompoundEdit(string name, params IEdit[] parts) { Name = name; _parts = parts; }

    public string Name { get; }
    public long Bytes => 32 + _parts.Sum(p => p.Bytes);

    public void Undo(Sprite s) { for (int i = _parts.Length - 1; i >= 0; i--) _parts[i].Undo(s); }
    public void Redo(Sprite s) { foreach (var p in _parts) p.Redo(s); }

    /// <summary>
    /// <paramref name="edit"/> as one step together with what the gesture also changed: the palette since
    /// <paramref name="paletteBefore"/> and a cel it had to create (<paramref name="celAdded"/>). Just <paramref name="edit"/>
    /// when there is nothing else.
    /// </summary>
    public static IEdit WithPalette(IEdit edit, PaletteSnapshot? paletteBefore, Sprite s, CelAddedEdit? celAdded = null)
    {
        var parts = new List<IEdit>();
        if (paletteBefore?.EditSince(s, edit.Name) is { } pal) parts.Add(pal);
        if (celAdded != null) parts.Add(celAdded);
        if (parts.Count == 0) return edit;
        parts.Add(edit);
        return new CompoundEdit(edit.Name, parts.ToArray());
    }
}

/// <summary>A cel made for a gesture on an empty layer/frame: undo takes it away again (so whole-sprite undo steps don't lose it).</summary>
public sealed class CelAddedEdit : IEdit
{
    private readonly Layer _layer;
    private readonly int _frame;
    private readonly Cel _cel;

    public CelAddedEdit(Layer layer, int frame, Cel cel) { _layer = layer; _frame = frame; _cel = cel; }

    public string Name => "Add cel";
    public long Bytes => 48;
    public void Undo(Sprite s) => s.Cels.Remove((_layer, _frame));
    public void Redo(Sprite s) => s.Cels[(_layer, _frame)] = _cel;
}
