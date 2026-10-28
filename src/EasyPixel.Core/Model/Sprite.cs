namespace EasyPixel.Core.Model;

/// <summary>The document: canvas size, colour mode, palette, layers, frames, tags and cels.</summary>
public sealed class Sprite
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ColorMode Mode { get; private set; }
    public Palette Palette { get; set; } = new();
    public List<Layer> Layers { get; } = new();
    public List<Frame> Frames { get; } = new();
    public List<Tag> Tags { get; } = new();
    public Dictionary<(Layer, int), Cel> Cels { get; } = new();

    /// <summary>Chunks of a file format we do not model, kept so saving can warn about them.</summary>
    public Dictionary<string, byte[]> UnknownChunks { get; } = new();

    /// <summary>A new sprite with one layer and one frame. Indexed sprites start with a transparent entry at index 0.</summary>
    public Sprite(int width, int height, ColorMode mode = ColorMode.Rgba)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Mode = mode;
        Layers.Add(new Layer());
        Frames.Add(new Frame());
        if (mode == ColorMode.Indexed)
        {
            Palette.Colors.Add(Rgba.Transparent);
            Palette.TransparentIndex = 0;
        }
    }

    /// <summary>The pixel value that means "nothing" in the current mode.</summary>
    public uint TransparentPixel => Mode == ColorMode.Indexed && Palette.TransparentIndex >= 0 ? (uint)Palette.TransparentIndex : 0u;

    public Cel GetOrCreateCel(Layer layer, int frame)
    {
        if (!Cels.TryGetValue((layer, frame), out var cel))
        {
            cel = new Cel(new Image(Width, Height, TransparentPixel));
            Cels[(layer, frame)] = cel;
        }
        return cel;
    }

    /// <summary>The value to store in an image to draw <paramref name="c"/>. Indexed: finds or adds a palette entry (nearest when full).</summary>
    public uint ColorToPixel(Rgba c)
    {
        if (Mode == ColorMode.Rgba) return c.Packed;
        if (c.A == 0 && Palette.TransparentIndex >= 0) return (uint)Palette.TransparentIndex;
        int i = Palette.IndexOf(c);
        if (i >= 0) return (uint)i;
        if (Palette.Colors.Count < Palette.MaxColors) return (uint)Palette.Add(c);
        return (uint)Palette.Nearest(c);
    }

    /// <summary>
    /// Removes a palette entry of an indexed sprite. Pixels using it are remapped to the nearest remaining colour
    /// (squared RGBA distance); indices above it shift down.
    /// </summary>
    public void RemovePaletteEntry(int index)
    {
        if (Mode != ColorMode.Indexed) throw new InvalidOperationException("Only indexed sprites have palette indices.");
        var colors = Palette.Colors;
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, colors.Count);
        if (colors.Count == 1) throw new InvalidOperationException("The last palette entry cannot be removed.");

        int replacement = Palette.Nearest(colors[index], skip: index);
        int shifted = replacement > index ? replacement - 1 : replacement;
        foreach (var cel in Cels.Values)
        {
            var px = cel.Image.Pixels;
            for (int i = 0; i < px.Length; i++)
            {
                uint v = px[i];
                if (v == (uint)index) px[i] = (uint)shifted;
                else if (v > (uint)index) px[i] = v - 1;
            }
        }
        colors.RemoveAt(index);
        int t = Palette.TransparentIndex;
        Palette.TransparentIndex = t == index ? -1 : t > index ? t - 1 : t;
    }

    /// <summary>
    /// Changes the canvas size without scaling. Cels keep their pixels; their positions shift so the content
    /// stays where the anchor says (content outside the new canvas is kept, just not visible).
    /// </summary>
    public void Resize(int width, int height, Anchor anchor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        int col = (int)anchor % 3, row = (int)anchor / 3; // 0 = left/top, 1 = centre, 2 = right/bottom
        int dx = (width - Width) * col / 2, dy = (height - Height) * row / 2;
        foreach (var cel in Cels.Values) { cel.X += dx; cel.Y += dy; }
        Width = width;
        Height = height;
    }

    public void ConvertMode(ColorMode to)
    {
        if (to == Mode) return;
        if (to == ColorMode.Indexed) RgbaToIndexed(); else IndexedToRgba();
        Mode = to;
    }

    private void RgbaToIndexed()
    {
        // Count colours; first-seen order makes ties and the palette order deterministic.
        var counts = new Dictionary<uint, int>();
        var order = new List<uint>();
        foreach (var cel in Cels.Values)
            foreach (uint p in cel.Image.Pixels)
            {
                if ((p >> 24) == 0) continue;
                if (counts.TryGetValue(p, out int n)) counts[p] = n + 1;
                else { counts[p] = 1; order.Add(p); }
            }

        int room = Palette.MaxColors - 1; // index 0 is the transparent entry
        var kept = order.Count <= room
            ? order
            : order.Select((p, i) => (p, i)).OrderByDescending(t => counts[t.p]).ThenBy(t => t.i).Take(room)
                .OrderBy(t => t.i).Select(t => t.p).ToList();

        var pal = new Palette();
        pal.Colors.Add(Rgba.Transparent);
        pal.TransparentIndex = 0;
        var map = new Dictionary<uint, uint>();
        foreach (uint p in kept) { map[p] = (uint)pal.Colors.Count; pal.Colors.Add(Rgba.FromPacked(p)); }

        foreach (var cel in Cels.Values)
        {
            var px = cel.Image.Pixels;
            for (int i = 0; i < px.Length; i++)
            {
                uint p = px[i];
                if ((p >> 24) == 0) { px[i] = 0; continue; }
                if (!map.TryGetValue(p, out uint idx))
                {
                    idx = (uint)pal.Nearest(Rgba.FromPacked(p), skip: 0);
                    map[p] = idx;
                }
                px[i] = idx;
            }
        }
        Palette = pal;
    }

    private void IndexedToRgba()
    {
        var colors = Palette.Colors;
        int t = Palette.TransparentIndex;
        foreach (var cel in Cels.Values)
        {
            var px = cel.Image.Pixels;
            for (int i = 0; i < px.Length; i++)
            {
                uint v = px[i];
                px[i] = (int)v == t || v >= (uint)colors.Count ? 0u : colors[(int)v].Packed;
            }
        }
    }
}
