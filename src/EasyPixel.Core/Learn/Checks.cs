using EasyPixel.Core.Model;

namespace EasyPixel.Core.Learn;

/// <summary>
/// One line of feedback. <see cref="Text"/> is English with <c>{0}</c> placeholders (the app shows it through its
/// translation table with <see cref="Args"/>); every text is written through <see cref="CheckRule.Say"/> so
/// tools/Update-Strings.sh finds it.
/// </summary>
public sealed record CheckLine(bool Ok, string Text, object[] Args)
{
    public string English => Args.Length == 0 ? Text : string.Format(System.Globalization.CultureInfo.InvariantCulture, Text, Args);
}

/// <summary>What <see cref="Exercise.Check"/> found: passed when every line is.</summary>
public sealed record CheckResult(IReadOnlyList<CheckLine> Lines)
{
    public bool Passed => Lines.All(l => l.Ok);
}

/// <summary>
/// What a rule looks at: the drawing (every layer but the guide, flattened to RGBA, one image per frame) and the
/// guide, flattened apart.
/// </summary>
public sealed class CheckInput
{
    public CheckInput(Sprite sprite, IReadOnlyList<Image> frames, Image guide)
    {
        Sprite = sprite;
        Frames = frames;
        Guide = guide;
    }

    public Sprite Sprite { get; }
    public IReadOnlyList<Image> Frames { get; }
    public Image Guide { get; }
    public int Width => Sprite.Width;
    public int Height => Sprite.Height;
    /// <summary>The first frame of the drawing.</summary>
    public Image Work => Frames[0];

    /// <summary>A drawing made of one RGBA image (tests and rule fixtures).</summary>
    public static CheckInput Of(params Image[] frames)
    {
        var s = new Sprite(frames[0].Width, frames[0].Height);
        for (int i = 1; i < frames.Length; i++) s.Frames.Add(new Frame());
        for (int i = 0; i < frames.Length; i++) s.Cels[(s.Layers[0], i)] = new Cel(frames[i]);
        return new CheckInput(s, frames, new Image(frames[0].Width, frames[0].Height));
    }
}

/// <summary>A rule that judges a drawing and says why, in one line.</summary>
public abstract class CheckRule
{
    public abstract CheckLine Check(CheckInput input);

    /// <summary>A line of feedback (see <see cref="CheckLine"/>).</summary>
    protected static CheckLine Say(bool ok, string text, params object[] args) => new(ok, text, args);

    protected static bool Opaque(uint v) => (v >> 24) != 0;

    protected static bool OpaqueAt(Image img, int x, int y) => (uint)x < (uint)img.Width && (uint)y < (uint)img.Height && Opaque(img.Get(x, y));

    protected static double Luma(uint v) => Pal.Luma(Rgba.FromPacked(v));

    protected static int CountOpaque(Image img) => img.Pixels.Count(Opaque);
}

/// <summary>The light direction a shading check expects.</summary>
public enum LightDir { TopLeft, Top, TopRight, Left, Right }

/// <summary>The rule library: build exercises' rules from these (see AGENTS.md, "Lessons").</summary>
public static class Rules
{
    /// <summary>At least <paramref name="minPixels"/> opaque pixels in every frame.</summary>
    public static CheckRule NotEmpty(int minPixels = 1) => new NotEmptyRule(minPixels);
    /// <summary>At most <paramref name="max"/> different colours (all frames).</summary>
    public static CheckRule MaxColors(int max) => new ColorCountRule(1, max);
    /// <summary>At least <paramref name="min"/> different colours (all frames).</summary>
    public static CheckRule MinColors(int min) => new ColorCountRule(min, int.MaxValue);
    /// <summary>Nothing within <paramref name="margin"/> pixels of the canvas border.</summary>
    public static CheckRule KeepsMargin(int margin = 1) => new MarginRule(margin);
    /// <summary>Opaque pixels cover between <paramref name="min"/> and <paramref name="max"/> of the canvas (0–1).</summary>
    public static CheckRule FillRatio(double min, double max) => new FillRatioRule(min, max);
    /// <summary>At most <paramref name="max"/> separate pieces (8-connected), so no stray pixels.</summary>
    public static CheckRule MaxParts(int max) => new PartsRule(max);
    /// <summary>Lines one pixel thick: no 2×2 block of opaque pixels.</summary>
    public static CheckRule NoClumps() => new NoClumpsRule();
    /// <summary>No "double" (L-shaped) corners in lines (at most <paramref name="allowed"/>).</summary>
    public static CheckRule PixelPerfect(int allowed = 0) => new PixelPerfectRule(allowed);
    /// <summary>
    /// The shape is outlined: at least <paramref name="share"/> of the opaque pixels touching transparency are
    /// <paramref name="color"/> (when given) or darker than the inside pixel next to them (black, coloured and sel-out
    /// outlines all pass).
    /// </summary>
    public static CheckRule HasOutline(Rgba? color = null, double share = 0.95) => new OutlineRule(color, share);
    /// <summary>The side of the shape facing <paramref name="dir"/> is lighter on average than the opposite side.</summary>
    public static CheckRule LightFrom(LightDir dir = LightDir.TopLeft) => new LightRule(dir);
    /// <summary>Every pixel is opaque (a ground tile has no holes).</summary>
    public static CheckRule FullyOpaque() => new FullyOpaqueRule();
    /// <summary>The image tiles without a visible seam (left/right and/or top/bottom edges fit together).</summary>
    public static CheckRule Seamless(bool horizontal = true, bool vertical = true) => new SeamlessRule(horizontal, vertical);
    /// <summary>At least <paramref name="min"/> frames, and (when <paramref name="movement"/>) each differs from the one before.</summary>
    public static CheckRule HasFrames(int min, bool movement = true) => new FramesRule(min, movement);
    /// <summary>The colours at <paramref name="points"/> go from dark to light, each step at least <paramref name="minStep"/> brighter (0–255).</summary>
    public static CheckRule RampOrdered(IReadOnlyList<(int X, int Y)> points, double minStep = 10) => new RampRule(points, minStep);
    /// <summary>The colour at <paramref name="light"/> is warmer (nearer yellow) than the one at <paramref name="dark"/>, by <paramref name="minDegrees"/> of hue.</summary>
    public static CheckRule HueShift((int X, int Y) dark, (int X, int Y) light, double minDegrees = 8) => new HueShiftRule(dark, light, minDegrees);
    /// <summary>At least <paramref name="minCells"/> 2×2 checkerboard cells (a b / b a) of two different colours.</summary>
    public static CheckRule UsesDither(int minCells = 4) => new DitherRule(minCells);
}

internal sealed class NotEmptyRule(int min) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        int least = input.Frames.Min(CountOpaque);
        if (least == 0) return Say(false, "The drawing layer is empty: draw on the \"Drawing\" layer, not on the guide.");
        return least >= min
            ? Say(true, "You drew {0} pixels.", least)
            : Say(false, "Only {0} pixels so far: draw a bit more (at least {1}).", least, min);
    }
}

internal sealed class ColorCountRule(int min, int max) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var colors = new HashSet<uint>();
        foreach (var f in input.Frames) foreach (uint v in f.Pixels) if (Opaque(v)) colors.Add(v);
        int n = colors.Count;
        if (n > max) return Say(false, "You used {0} colours; the limit is {1}. Merge the shades that look almost the same.", n, max);
        if (n < min) return Say(false, "You used {0} colours; use at least {1}.", n, min);
        return max == int.MaxValue
            ? Say(true, "{0} colours: enough to give it depth.", n)
            : Say(true, "{0} colours, within the limit of {1}.", n, max);
    }
}

internal sealed class MarginRule(int margin) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        foreach (var f in input.Frames)
            for (int y = 0; y < f.Height; y++)
                for (int x = 0; x < f.Width; x++)
                    if (Opaque(f.Get(x, y)) && (x < margin || y < margin || x >= f.Width - margin || y >= f.Height - margin))
                        return Say(false, "The drawing touches the canvas border (at {0}, {1}): leave a margin so nothing gets cut off.", x, y);
        return Say(true, "The drawing fits inside the canvas with a margin.");
    }
}

internal sealed class FillRatioRule(double min, double max) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        double r = (double)CountOpaque(input.Work) / (input.Work.Width * input.Work.Height);
        int pct = (int)Math.Round(r * 100);
        if (r < min) return Say(false, "The shape covers {0}% of the canvas: make it bigger and bolder (at least {1}%).", pct, (int)Math.Round(min * 100));
        if (r > max) return Say(false, "The shape covers {0}% of the canvas: it reads as a blob. Carve out gaps between parts (at most {1}%).", pct, (int)Math.Round(max * 100));
        return Say(true, "The shape covers {0}% of the canvas: a good size.", pct);
    }
}

internal sealed class PartsRule(int max) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        var seen = new bool[img.Width * img.Height];
        int parts = 0;
        (int, int)? smallest = null;
        int smallestSize = int.MaxValue;
        var stack = new Stack<(int, int)>();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (seen[y * img.Width + x] || !Opaque(img.Get(x, y))) continue;
                parts++;
                int size = 0;
                stack.Push((x, y));
                seen[y * img.Width + x] = true;
                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    size++;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = cx + dx, ny = cy + dy;
                            if (!OpaqueAt(img, nx, ny) || seen[ny * img.Width + nx]) continue;
                            seen[ny * img.Width + nx] = true;
                            stack.Push((nx, ny));
                        }
                }
                if (size < smallestSize) { smallestSize = size; smallest = (x, y); }
            }
        if (parts <= max) return Say(true, "No stray pixels: the drawing is {0} clean piece(s).", parts);
        return Say(false, "The drawing is in {0} separate pieces (at most {1}): look for stray pixels, e.g. at {2}, {3}.",
            parts, max, smallest!.Value.Item1, smallest.Value.Item2);
    }
}

internal sealed class NoClumpsRule : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        for (int y = 0; y < img.Height - 1; y++)
            for (int x = 0; x < img.Width - 1; x++)
                if (OpaqueAt(img, x, y) && OpaqueAt(img, x + 1, y) && OpaqueAt(img, x, y + 1) && OpaqueAt(img, x + 1, y + 1))
                    return Say(false, "There's a 2×2 clump at {0}, {1}: lines should be one pixel thick.", x, y);
        return Say(true, "Every line is one pixel thick.");
    }
}

internal sealed class PixelPerfectRule(int allowed) : CheckRule
{
    /// <summary>
    /// The pixels that make a "double": a pixel joining a horizontal and a vertical neighbour that already touch
    /// diagonally, where the line doesn't go on straight both ways (a real square corner does).
    /// </summary>
    public static List<(int X, int Y)> Doubles(Image img)
    {
        var found = new List<(int, int)>();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (!OpaqueAt(img, x, y)) continue;
                int n4 = (OpaqueAt(img, x - 1, y) ? 1 : 0) + (OpaqueAt(img, x + 1, y) ? 1 : 0) + (OpaqueAt(img, x, y - 1) ? 1 : 0) + (OpaqueAt(img, x, y + 1) ? 1 : 0);
                if (n4 != 2) continue;
                foreach (int hx in new[] { -1, 1 })
                    foreach (int vy in new[] { -1, 1 })
                    {
                        if (!OpaqueAt(img, x + hx, y) || !OpaqueAt(img, x, y + vy) || OpaqueAt(img, x + hx, y + vy)) continue;
                        bool armH = OpaqueAt(img, x + 2 * hx, y), armV = OpaqueAt(img, x, y + 2 * vy);
                        if (!(armH && armV)) found.Add((x, y));
                    }
            }
        return found;
    }

    public override CheckLine Check(CheckInput input)
    {
        var d = Doubles(input.Work);
        if (d.Count <= allowed) return Say(true, "Clean lines: no double corners.");
        return Say(false, "{0} double corner(s) make the line look thick and jagged. Remove the corner pixel of each L, e.g. at {1}, {2}.",
            d.Count, d[0].X, d[0].Y);
    }
}

internal sealed class OutlineRule(Rgba? color, double share) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int edges = 0, ok = 0, interior = 0;
        (int, int)? firstGap = null;
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (!OpaqueAt(img, x, y)) continue;
                if (!IsEdge(img, x, y)) { interior++; continue; }
                edges++;
                if (EdgeOk(img, x, y)) ok++;
                else firstGap ??= (x, y);
            }
        if (edges == 0) return Say(false, "The drawing layer is empty: draw on the \"Drawing\" layer, not on the guide.");
        if (interior == 0) return Say(false, "There's no fill inside the outline: an outline goes around a filled shape.");
        if (ok >= edges * share) return Say(true, "The shape is outlined all the way around.");
        return Say(false, "{0} of {1} edge pixels have no outline. Close the gaps, e.g. at {2}, {3}.",
            edges - ok, edges, firstGap!.Value.Item1, firstGap.Value.Item2);
    }

    private static bool IsEdge(Image img, int x, int y) =>
        !OpaqueAt(img, x - 1, y) || !OpaqueAt(img, x + 1, y) || !OpaqueAt(img, x, y - 1) || !OpaqueAt(img, x, y + 1);

    private bool EdgeOk(Image img, int x, int y)
    {
        uint v = img.Get(x, y);
        if (color is { } c) return v == c.Packed;
        // Darker than an inside pixel next to it; a thin part with nothing inside next to it can't be judged.
        double l = Luma(v), brightest = -1;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if ((dx == 0 && dy == 0) || !OpaqueAt(img, nx, ny) || IsEdge(img, nx, ny)) continue;
                brightest = Math.Max(brightest, Luma(img.Get(nx, ny)));
            }
        return brightest < 0 || l < brightest - 4;
    }
}

internal sealed class LightRule(LightDir dir) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        var (lx, ly) = dir switch
        {
            LightDir.Top => (0.0, -1.0),
            LightDir.TopRight => (1.0, -1.0),
            LightDir.Left => (-1.0, 0.0),
            LightDir.Right => (1.0, 0.0),
            _ => (-1.0, -1.0),
        };
        // Inside pixels only, so an outline (dark all around) doesn't count as shading.
        var pts = new List<(int X, int Y, double L)>();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (OpaqueAt(img, x, y) && OpaqueAt(img, x - 1, y) && OpaqueAt(img, x + 1, y) && OpaqueAt(img, x, y - 1) && OpaqueAt(img, x, y + 1))
                    pts.Add((x, y, Luma(img.Get(x, y))));
        if (pts.Count < 8) return Say(false, "Draw a bigger filled shape to shade.");
        double cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
        double lit = 0, shade = 0;
        int nl = 0, ns = 0;
        foreach (var p in pts)
        {
            double d = (p.X - cx) * lx + (p.Y - cy) * ly;
            if (d > 0.5) { lit += p.L; nl++; }
            else if (d < -0.5) { shade += p.L; ns++; }
        }
        if (nl == 0 || ns == 0) return Say(false, "Draw a bigger filled shape to shade.");
        double diff = lit / nl - shade / ns;
        if (diff >= 12) return Say(true, "The light side faces the light and the far side falls into shadow.");
        if (diff <= -12) return Say(false, "The shading is backwards: the side facing the light is darker. Swap your light and shadow colours.");
        return Say(false, "Both sides are about as bright: that's pillow shading. Make the side facing the light lighter and the far side darker.");
    }
}

internal sealed class FullyOpaqueRule : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int holes = img.Pixels.Count(v => !Opaque(v));
        if (holes == 0) return Say(true, "Every pixel is filled.");
        int i = Array.FindIndex(img.Pixels, v => !Opaque(v));
        return Say(false, "{0} pixels are still empty (e.g. at {1}, {2}): a ground tile has no holes.", holes, i % img.Width, i / img.Width);
    }
}

internal sealed class SeamlessRule(bool horizontal, bool vertical) : CheckRule
{
    /// <summary>A step between neighbouring pixels this big (RGB distance) reads as an edge.</summary>
    private const double Break = 48;

    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        if (horizontal && !Fits(img, true)) return Say(false, "The left and right edges don't match: a seam shows when the tile repeats side by side. Try View → Tiled preview.");
        if (vertical && !Fits(img, false)) return Say(false, "The top and bottom edges don't match: a seam shows when the tile repeats up and down. Try View → Tiled preview.");
        return Say(true, "The tile repeats without a visible seam.");
    }

    /// <summary>
    /// Compares the step across the wrap (last column → first column, or last row → first row) with the steps inside
    /// the tile: the seam may not be much harsher than the tile's own texture.
    /// </summary>
    internal static bool Fits(Image img, bool across)
    {
        int n = across ? img.Width : img.Height, m = across ? img.Height : img.Width;
        if (n < 2) return true;
        uint At(int i, int j) => across ? img.Get(i, j) : img.Get(j, i);
        double insideSum = 0;
        int insideBreaks = 0;
        for (int i = 0; i < n - 1; i++)
            for (int j = 0; j < m; j++)
            {
                double d = Dist(At(i, j), At(i + 1, j));
                insideSum += d;
                if (d > Break) insideBreaks++;
            }
        double insideMean = insideSum / ((n - 1) * m);
        double insideBreaksPerLine = (double)insideBreaks / (n - 1);
        double seamSum = 0;
        int seamBreaks = 0;
        for (int j = 0; j < m; j++)
        {
            double d = Dist(At(n - 1, j), At(0, j));
            seamSum += d;
            if (d > Break) seamBreaks++;
        }
        double seamMean = seamSum / m;
        return seamBreaks <= Math.Max(2, insideBreaksPerLine * 1.5 + 1) && seamMean <= insideMean * 2 + 16;
    }

    private static double Dist(uint a, uint b)
    {
        var x = Rgba.FromPacked(a);
        var y = Rgba.FromPacked(b);
        return Math.Sqrt(x.DistanceSquared(y));
    }
}

internal sealed class FramesRule(int min, bool movement) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        int n = input.Frames.Count;
        if (n < min) return Say(false, "The animation has {0} frame(s); it needs at least {1}. Add frames with Frame → New frame.", n, min);
        if (movement)
            for (int i = 1; i < n; i++)
                if (input.Frames[i].Pixels.AsSpan().SequenceEqual(input.Frames[i - 1].Pixels))
                    return Say(false, "Frames {0} and {1} are the same: change something between them so it moves.", i, i + 1);
        return Say(true, "{0} frames, each one different.", n);
    }
}

internal sealed class RampRule(IReadOnlyList<(int X, int Y)> points, double minStep) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        double prev = -1;
        for (int i = 0; i < points.Count; i++)
        {
            uint v = input.Work.Get(points[i].X, points[i].Y);
            if (!Opaque(v)) return Say(false, "Swatch {0} is empty: fill every box of the ramp.", i + 1);
            double l = Luma(v);
            if (i > 0 && l < prev + minStep)
                return Say(false, "Swatch {0} isn't lighter than swatch {1}: a ramp goes from dark to light, one clear step at a time.", i + 1, i);
            prev = l;
        }
        return Say(true, "The ramp goes from dark to light in clear steps.");
    }
}

internal sealed class HueShiftRule((int X, int Y) dark, (int X, int Y) light, double minDegrees) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        uint d = input.Work.Get(dark.X, dark.Y), l = input.Work.Get(light.X, light.Y);
        if (!Opaque(d) || !Opaque(l)) return Say(false, "Fill the darkest and the lightest swatch first.");
        var (hd, sd) = Hue(Rgba.FromPacked(d));
        var (hl, sl) = Hue(Rgba.FromPacked(l));
        if (sd < 0.08 || sl < 0.08) return Say(false, "Those swatches are grey: hue shifting needs colour.");
        double toYellowDark = Distance(hd, 60), toYellowLight = Distance(hl, 60);
        if (toYellowDark - toYellowLight >= minDegrees) return Say(true, "The light end leans warm and the dark end leans cool: nice hue shift.");
        return Say(false, "The light end isn't warmer than the dark end. Nudge the lights toward yellow and the shadows toward blue or purple.");
    }

    private static double Distance(double a, double b)
    {
        double d = Math.Abs(a - b) % 360;
        return d > 180 ? 360 - d : d;
    }

    /// <summary>Hue in degrees and saturation 0–1.</summary>
    internal static (double Hue, double Sat) Hue(Rgba c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
        if (delta <= 0) return (0, 0);
        double h = max == r ? 60 * (((g - b) / delta) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        if (h < 0) h += 360;
        return (h, max <= 0 ? 0 : delta / max);
    }
}

internal sealed class DitherRule(int minCells) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int cells = 0;
        for (int y = 0; y < img.Height - 1; y++)
            for (int x = 0; x < img.Width - 1; x++)
            {
                uint a = img.Get(x, y), b = img.Get(x + 1, y);
                if (!Opaque(a) || !Opaque(b) || a == b) continue;
                if (img.Get(x, y + 1) == b && img.Get(x + 1, y + 1) == a) cells++;
            }
        return cells >= minCells
            ? Say(true, "You used a dither pattern to blend two colours.")
            : Say(false, "No dither pattern yet: alternate two colours in a checkerboard where they meet.");
    }
}
