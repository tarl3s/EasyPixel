using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Learn;

/// <summary>
/// The 16 colours lessons draw with (the "Sweetie 16" palette), each with a letter for <see cref="ArtCanvas.Rows"/>.
/// <c>.</c> (or a space) is transparent.
/// </summary>
public static class Pal
{
    public static readonly Rgba Black = Hex("1a1c2c"), Plum = Hex("5d275d"), Red = Hex("b13e53"), Orange = Hex("ef7d57"),
        Yellow = Hex("ffcd75"), Lime = Hex("a7f070"), Green = Hex("38b764"), Teal = Hex("257179"), Navy = Hex("29366f"),
        Blue = Hex("3b5dc9"), Sky = Hex("41a6f6"), Aqua = Hex("73eff7"), White = Hex("f4f4f4"), Silver = Hex("94b0c2"),
        Slate = Hex("566c86"), Ebony = Hex("333c57");

    /// <summary>All 16, dark to light where it matters (the order of the practice palettes).</summary>
    public static readonly Rgba[] All =
        { Black, Ebony, Navy, Slate, Silver, White, Plum, Red, Orange, Yellow, Lime, Green, Teal, Blue, Sky, Aqua };

    /// <summary>Letter → colour for <see cref="ArtCanvas.Rows"/>.</summary>
    public static readonly IReadOnlyDictionary<char, Rgba> Letters = new Dictionary<char, Rgba>
    {
        ['k'] = Black, ['p'] = Plum, ['r'] = Red, ['o'] = Orange, ['y'] = Yellow, ['l'] = Lime, ['g'] = Green,
        ['t'] = Teal, ['n'] = Navy, ['b'] = Blue, ['c'] = Sky, ['a'] = Aqua, ['w'] = White, ['s'] = Silver,
        ['d'] = Slate, ['e'] = Ebony,
    };

    /// <summary>A colour from "rrggbb" (or "#rrggbb"); opaque.</summary>
    public static Rgba Hex(string hex)
    {
        hex = hex.TrimStart('#');
        uint v = Convert.ToUInt32(hex, 16);
        return new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
    }

    /// <summary>Perceived brightness 0–255.</summary>
    public static double Luma(Rgba c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;

    /// <summary>A mix of two colours (t = 0 → a, 1 → b), opaque.</summary>
    public static Rgba Mix(Rgba a, Rgba b, double t) =>
        new((byte)Math.Round(a.R + (b.R - a.R) * t), (byte)Math.Round(a.G + (b.G - a.G) * t), (byte)Math.Round(a.B + (b.B - a.B) * t), 255);
}

/// <summary>
/// A small RGBA drawing surface for lesson illustrations and practice documents, built on the Core raster tools so
/// the art is crisp. Several frames make an animation (<see cref="NewFrame"/>).
/// </summary>
public sealed class ArtCanvas
{
    private readonly List<Image> _frames = new();
    private readonly List<int> _durations = new();

    public ArtCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        _frames.Add(new Image(width, height));
        _durations.Add(100);
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>The frame being drawn.</summary>
    public Image Image => _frames[^1];
    public IReadOnlyList<Image> Frames => _frames;

    /// <summary>Starts another frame (a copy of the current one when <paramref name="copy"/>); returns this.</summary>
    public ArtCanvas NewFrame(bool copy = false, int durationMs = 100)
    {
        _frames.Add(copy ? Image.Clone() : new Image(Width, Height));
        _durations.Add(durationMs);
        return this;
    }

    /// <summary>The current frame's duration.</summary>
    public ArtCanvas Duration(int ms)
    {
        _durations[^1] = ms;
        return this;
    }

    private PixelSink Sink => new(Image);

    public ArtCanvas Px(int x, int y, Rgba c)
    {
        Image.Set(x, y, c.Packed);
        return this;
    }

    public Rgba Get(int x, int y) => Rgba.FromPacked(Image.Get(x, y));

    public bool IsSet(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height && (Image.Get(x, y) >> 24) != 0;

    /// <summary>A one-pixel line; <paramref name="pixelPerfect"/> drops the L-shaped corner pixels.</summary>
    public ArtCanvas Line(int x0, int y0, int x1, int y1, Rgba c, bool pixelPerfect = true)
    {
        Shapes.Line(Sink, c.Packed, x0, y0, x1, y1, new Brush(), pixelPerfect);
        return this;
    }

    /// <summary>
    /// A pixel-perfect arc (no L-shaped corners) of the ellipse centred on (<paramref name="cx"/>, <paramref name="cy"/>)
    /// with radii <paramref name="rx"/>, <paramref name="ry"/>, from <paramref name="fromDeg"/> to <paramref name="toDeg"/>
    /// (0 = right, 90 = down).
    /// </summary>
    public ArtCanvas Arc(double cx, double cy, double rx, double ry, double fromDeg, double toDeg, Rgba c)
    {
        var pts = new List<(int X, int Y)>();
        int steps = (int)Math.Ceiling(Math.Abs(toDeg - fromDeg) * Math.Max(rx, ry) / 8) + 8;
        for (int i = 0; i <= steps; i++)
        {
            double a = (fromDeg + (toDeg - fromDeg) * i / steps) * Math.PI / 180;
            var p = ((int)Math.Round(cx + rx * Math.Cos(a)), (int)Math.Round(cy + ry * Math.Sin(a)));
            if (pts.Count > 0 && pts[^1] == p) continue;
            pts.Add(p);
            // The pixel before last is a corner when the new one already touches the one before it.
            while (pts.Count >= 3 && Math.Abs(pts[^3].X - pts[^1].X) <= 1 && Math.Abs(pts[^3].Y - pts[^1].Y) <= 1) pts.RemoveAt(pts.Count - 2);
        }
        foreach (var (x, y) in pts)
            if ((uint)x < (uint)Width && (uint)y < (uint)Height) Px(x, y, c);
        return this;
    }

    /// <summary>A rectangle with both corners included.</summary>
    public ArtCanvas Rect(int x0, int y0, int x1, int y1, Rgba c, bool filled = true)
    {
        Shapes.Rect(Sink, c.Packed, x0, y0, x1, y1, new Brush(), filled);
        return this;
    }

    /// <summary>The ellipse inscribed in the box (x0,y0)-(x1,y1).</summary>
    public ArtCanvas Ellipse(int x0, int y0, int x1, int y1, Rgba c, bool filled = true)
    {
        Shapes.Ellipse(Sink, c.Packed, x0, y0, x1, y1, new Brush(), filled);
        return this;
    }

    /// <summary>
    /// Draws text rows from (x, y): each character is a pixel, looked up in <paramref name="legend"/> first and then in
    /// <see cref="Pal.Letters"/>; '.' and ' ' are left untouched.
    /// </summary>
    public ArtCanvas Rows(int x, int y, IReadOnlyDictionary<char, Rgba>? legend, params string[] rows)
    {
        for (int j = 0; j < rows.Length; j++)
            for (int i = 0; i < rows[j].Length; i++)
            {
                char ch = rows[j][i];
                if (ch is '.' or ' ') continue;
                if (legend != null && legend.TryGetValue(ch, out var c) || Pal.Letters.TryGetValue(ch, out c)) Px(x + i, y + j, c);
            }
        return this;
    }

    public ArtCanvas Rows(int x, int y, params string[] rows) => Rows(x, y, null, rows);

    /// <summary>Recolours every pixel of the current frame for which <paramref name="paint"/> returns a colour.</summary>
    public ArtCanvas Shade(Func<int, int, Rgba, Rgba?> paint)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var c = Get(x, y);
                if (c.A == 0) continue;
                if (paint(x, y, c) is { } n) Px(x, y, n);
            }
        return this;
    }

    /// <summary>
    /// Shades a filled ellipse as a lit sphere: the light comes from (<paramref name="lx"/>, <paramref name="ly"/>)
    /// (e.g. -1, -1 = top-left); <paramref name="ramp"/> goes dark → light.
    /// </summary>
    public ArtCanvas Sphere(int x0, int y0, int x1, int y1, IReadOnlyList<Rgba> ramp, double lx = -1, double ly = -1)
    {
        double cx = (x0 + x1) / 2.0, cy = (y0 + y1) / 2.0, rx = (x1 - x0 + 1) / 2.0, ry = (y1 - y0 + 1) / 2.0;
        double len = Math.Sqrt(lx * lx + ly * ly + 1);
        double Lx = lx / len, Ly = ly / len, Lz = 1 / len;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                double nx = (x + 0.5 - cx - 0.0) / rx, ny = (y + 0.5 - cy) / ry;
                double d = nx * nx + ny * ny;
                if (d > 1) continue;
                double nz = Math.Sqrt(1 - d);
                double light = Math.Max(0, nx * Lx + ny * Ly + nz * Lz);
                int i = Math.Clamp((int)(light * ramp.Count), 0, ramp.Count - 1);
                Px(x, y, ramp[i]);
            }
        return this;
    }

    /// <summary>Puts a one-pixel outline of <paramref name="c"/> around everything drawn (outside, no diagonals).</summary>
    public ArtCanvas Outline(Rgba c, bool diagonal = false)
    {
        Raster.Outline.Apply(Sink, c.Packed, outside: true, diagonal: diagonal);
        return this;
    }

    /// <summary>
    /// A selective outline: every transparent pixel next to the drawing gets the colour <paramref name="pick"/> chooses
    /// for it (x, y, the colour it touches); by default a darker version of that colour, so the outline follows the light.
    /// </summary>
    public ArtCanvas SelOut(Func<int, int, Rgba, Rgba>? pick = null)
    {
        pick ??= (_, _, near) => Pal.Mix(near, Pal.Black, 0.45);
        var src = Image.Clone();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if ((src.Get(x, y) >> 24) != 0) continue;
                Rgba? near = null;
                foreach (var (dx, dy) in Four)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((uint)nx < (uint)Width && (uint)ny < (uint)Height && (src.Get(nx, ny) >> 24) != 0)
                    {
                        near = Rgba.FromPacked(src.Get(nx, ny));
                        break;
                    }
                }
                if (near is { } n) Px(x, y, pick(x, y, n));
            }
        return this;
    }

    private static readonly (int, int)[] Four = { (0, -1), (-1, 0), (1, 0), (0, 1) };

    /// <summary>Copies a rectangle of the current frame to (dx, dy) (transparent pixels skipped).</summary>
    public ArtCanvas Copy(int x, int y, int w, int h, int dx, int dy)
    {
        var src = Image.Clone();
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                uint v = src.Get(x + i, y + j);
                if ((v >> 24) != 0) Image.Set(dx + i, dy + j, v);
            }
        return this;
    }

    /// <summary>A one-layer RGBA sprite with one frame per <see cref="NewFrame"/>.</summary>
    public Sprite ToSprite()
    {
        var s = new Sprite(Width, Height);
        for (int f = 0; f < _frames.Count; f++)
        {
            if (f > 0) s.Frames.Add(new Frame());
            s.Frames[f].DurationMs = _durations[f];
            s.Cels[(s.Layers[0], f)] = new Cel(_frames[f].Clone());
        }
        return s;
    }

    /// <summary>Copies the frames into <paramref name="layer"/> of <paramref name="sprite"/> (same size; frames past the sprite's are dropped).</summary>
    public void CopyInto(Sprite sprite, Layer layer)
    {
        for (int f = 0; f < Math.Min(_frames.Count, sprite.Frames.Count); f++)
            sprite.Cels[(layer, f)] = new Cel(_frames[f].Clone());
    }
}
