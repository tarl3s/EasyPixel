using EasyPixel.Core.Model;

namespace EasyPixel.Core.Learn;

/// <summary>
/// Every practice exercise by id. A lesson's <c>[[exercise:id]]</c> must name one of these (tested). To add one, add an
/// entry to <see cref="All"/> (see AGENTS.md, "Lessons").
/// </summary>
public static class Exercises
{
    private static readonly Rgba Faint = Pal.Silver, Mark = Pal.Red;

    /// <summary>The centres of the five ramp boxes of "palette-ramp", dark to light (declared before <see cref="All"/>, which uses them).</summary>
    internal static readonly (int X, int Y)[] RampSwatches = { (4, 4), (12, 4), (20, 4), (28, 4), (36, 4) };

    public static readonly IReadOnlyList<Exercise> All = new List<Exercise>
    {
        // 01 Pixels and canvas size: a first 16×16 sprite with a tiny palette.
        new()
        {
            Id = "first-sprite", Width = 16, Height = 16,
            Guide = g => g.Rect(1, 1, 14, 14, Faint, filled: false),
            Rules = new[] { Rules.NotEmpty(40), Rules.MaxColors(4), Rules.KeepsMargin(1), Rules.MaxParts(3) },
            Palette = new[] { Pal.Black, Pal.White, Pal.Red, Pal.Orange, Pal.Yellow, Pal.Green, Pal.Blue, Pal.Silver },
        },
        // 02 Lines and curves: three slopes between the red dots, and a curve along the dotted arc.
        new()
        {
            Id = "clean-lines", Width = 32, Height = 32,
            Guide = g =>
            {
                foreach (var (x, y) in new[] { (2, 2), (9, 9), (12, 2), (27, 9), (2, 13), (19, 18) }) g.Px(x, y, Mark);
                for (int a = 0; a <= 90; a += 6) // a quarter circle around (30, 30), radius 12, every other step
                {
                    double r = a * Math.PI / 180;
                    g.Px((int)Math.Round(30 - 12 * Math.Cos(r)), (int)Math.Round(30 - 12 * Math.Sin(r)), Faint);
                }
                g.Px(18, 30, Mark).Px(30, 18, Mark);
            },
            Rules = new[] { Rules.NotEmpty(40), Rules.MaxColors(2), Rules.NoClumps(), Rules.PixelPerfect() },
            Palette = new[] { Pal.Black, Pal.Navy, Pal.Blue, Pal.Sky, Pal.White },
        },
        // 03 Shapes and silhouettes: one colour, a bold readable shape.
        new()
        {
            Id = "silhouette", Width = 32, Height = 32,
            Guide = g => g.Line(2, 29, 29, 29, Faint),
            Rules = new[] { Rules.NotEmpty(80), Rules.MaxColors(1), Rules.FillRatio(0.12, 0.6), Rules.MaxParts(2), Rules.KeepsMargin(1) },
            Palette = new[] { Pal.Black, Pal.Ebony, Pal.Navy },
        },
        // 04 Colour palettes: a five-colour ramp in the boxes, dark to light, hue-shifted.
        new()
        {
            Id = "palette-ramp", Width = 42, Height = 10,
            Guide = g => { for (int i = 0; i < 5; i++) g.Rect(1 + i * 8, 1, 8 + i * 8, 8, Faint, filled: false); },
            Rules = new[] { Rules.NotEmpty(100), Rules.MaxColors(6), Rules.RampOrdered(RampSwatches, 15), Rules.HueShift(RampSwatches[0], RampSwatches[^1], 15), Rules.Contrast(min: 120) },
        },
        // 05 Light and shading: shade the flat ball with the light coming from the top-left (the sun mark).
        new()
        {
            Id = "shade-ball", Width = 32, Height = 32,
            Guide = g => g.Ellipse(3, 3, 28, 28, Faint, filled: false).Rows(0, 0, ".y.", "yyy", ".y."),
            Start = w => w.Ellipse(4, 4, 27, 27, Pal.Orange),
            Rules = new[] { Rules.NotEmpty(200), Rules.MinColors(3), Rules.MaxColors(6), Rules.LightFrom(LightDir.TopLeft) },
            Palette = new[] { Pal.Black, Pal.Navy, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.White, Pal.Slate },
        },
        // 06 Outlines: the apple has no outline yet.
        new()
        {
            Id = "outline-apple", Width = 32, Height = 32,
            Start = w => Apple(w, 6, 6),
            Rules = new[] { Rules.HasOutline(), Rules.MaxColors(10) },
            Palette = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Teal, Pal.Green, Pal.Lime, Pal.Ebony, Pal.Navy },
        },
        // 07 Anti-aliasing: smooth the curve of the black shape on white, leaving the straight sides alone.
        new()
        {
            Id = "aa-curve", Width = 32, Height = 32,
            Start = AaStart,
            Rules = new[] { Rules.AntiAliased(Pal.Black, Pal.White, 6), Rules.MaxColors(5) },
            Palette = new[] { Pal.Black, Pal.Ebony, Pal.Slate, Pal.Silver, Pal.White },
        },
        // 08 Dithering: a sky that goes from dark at the top to light at the horizon with very few colours.
        new()
        {
            Id = "dither-sky", Width = 32, Height = 24,
            Rules = new[] { Rules.FullyOpaque(), Rules.MinColors(2), Rules.MaxColors(4), Rules.UsesDither(8), Rules.GetsLighter(Toward.Down) },
            Palette = new[] { Pal.Black, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Aqua, Pal.Plum, Pal.Orange, Pal.Yellow },
        },
        // 09 Textures and materials: a 16×16 block of one material, textured but not noisy.
        new()
        {
            Id = "material-block", Width = 16, Height = 16,
            Rules = new[] { Rules.FullyOpaque(), Rules.MinColors(3), Rules.MaxColors(5), Rules.Texture(0.1, 0.45) },
            Palette = new[] { Pal.Black, Pal.Ebony, Pal.Slate, Pal.Silver, Pal.White, Pal.Plum, Pal.Red, Pal.Orange, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Teal, Pal.Green, Pal.Lime },
        },
        // 10 Animation basics: a ball that bounces on the floor line.
        new()
        {
            Id = "bouncing-ball", Width = 32, Height = 32,
            Guide = g => g.Line(0, 30, 31, 30, Faint),
            Rules = new[] { Rules.NotEmpty(12), Rules.HasFrames(5), Rules.Bounces(29, 10), Rules.MaxColors(6) },
            Palette = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.White, Pal.Navy, Pal.Blue, Pal.Sky },
        },
        // 11 Walk cycle: four or more frames walking in place on the floor line.
        new()
        {
            Id = "walk-cycle", Width = 24, Height = 32,
            Guide = g => { g.Line(0, 30, 23, 30, Faint); for (int x = 0; x < 24; x += 2) g.Px(x, 6, Faint); },
            Rules = new[] { Rules.NotEmpty(40), Rules.HasFrames(4), Rules.OnFloor(29), Rules.InPlace(3), Rules.Bobs(1) },
            Palette = new[] { Pal.Black, Pal.Plum, Pal.Orange, Pal.Yellow, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Ebony, Pal.Slate, Pal.White },
        },
        // 12 Character sprites: a hero standing on the floor line, outlined, with a small palette.
        new()
        {
            Id = "hero-sprite", Width = 24, Height = 32,
            Guide = g => { g.Line(0, 30, 23, 30, Faint); for (int x = 0; x < 24; x += 2) g.Px(x, 17, Faint); }, // the floor, and about where a chibi's chin goes
            Rules = new[] { Rules.NotEmpty(120), Rules.MinColors(3), Rules.MaxColors(8), Rules.HasOutline(), Rules.MaxParts(2), Rules.OnFloor(29), Rules.KeepsMargin(1) },
            Palette = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.White, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Ebony, Pal.Teal, Pal.Green },
        },
        // 13 Tilesets: a 16×16 ground tile that repeats without a seam.
        new()
        {
            Id = "seamless-tile", Width = 16, Height = 16,
            Rules = new[] { Rules.FullyOpaque(), Rules.MinColors(3), Rules.MaxColors(6), Rules.Seamless() },
            Palette = new[] { Pal.Black, Pal.Ebony, Pal.Teal, Pal.Green, Pal.Lime, Pal.Plum, Pal.Red, Pal.Orange, Pal.Slate, Pal.Silver },
        },
        // 14 Backgrounds and parallax: a far hills layer that repeats sideways, low in contrast, reaching the bottom.
        new()
        {
            Id = "parallax-hills", Width = 64, Height = 32,
            Rules = new[] { Rules.NotEmpty(300), Rules.Seamless(horizontal: true, vertical: false), Rules.FilledBottom(), Rules.MaxColors(3), Rules.Contrast(max: 70) },
            Palette = new[] { Pal.Slate, Pal.Silver, Pal.Sky, Pal.Aqua, Pal.Teal, Pal.Green, Pal.Navy, Pal.Blue, Pal.White },
        },
        // 15 Buildings and props: a school (or any building) in side view, with matching copied windows, on the ground line.
        new()
        {
            Id = "school-front", Width = 48, Height = 48,
            Guide = g => g.Line(0, 46, 47, 46, Faint),
            Rules = new[] { Rules.NotEmpty(400), Rules.MinColors(4), Rules.MaxColors(12), Rules.OnFloor(45), Rules.Repeats(3), Rules.MaxParts(3) },
        },
        // 16 Items and icons: a 16×16 item icon that reads at 1×.
        new()
        {
            Id = "item-icon", Width = 16, Height = 16,
            Rules = new[] { Rules.NotEmpty(50), Rules.KeepsMargin(1), Rules.UsesTheSpace(0.7), Rules.HasOutline(), Rules.MinColors(3), Rules.MaxColors(8), Rules.MaxParts(1), Rules.Contrast(min: 100) },
        },
        // 17 Effects: a one-shot explosion or puff that grows and fades out.
        new()
        {
            Id = "explosion", Width = 24, Height = 24,
            Rules = new[] { Rules.HasFrames(5), Rules.GrowsThenFades(), Rules.MinColors(3), Rules.MaxColors(6) },
            Palette = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.White, Pal.Ebony, Pal.Slate, Pal.Silver },
        },
        // 18 Making it game-ready: six drawn frames (idle and walk) to name with tags before exporting.
        new()
        {
            Id = "game-ready", Width = 16, Height = 22, Frames = 6,
            Start = w =>
            {
                LessonArt.Slime(w, 1, 8, 14, 13);
                w.NewFrame();
                LessonArt.Slime(w, 1, 10, 14, 11);
                for (int f = 0; f < 4; f++)
                {
                    w.NewFrame();
                    LessonArt.Walker(w, f, 1, 1);
                }
            },
            Rules = new[] { Rules.HasTags(2), Rules.HasFrames(6), Rules.OnFloor(20) },
        },
    };

    /// <summary>"aa-curve" starts with a quarter-pie outline on white: two straight sides and a curve to smooth.</summary>
    public static void AaStart(ArtCanvas c)
    {
        c.Rect(0, 0, 31, 31, Pal.White);
        c.Arc(28, 28, 23, 23, 180, 270, Pal.Black);
        c.Line(5, 28, 28, 28, Pal.Black).Line(28, 5, 28, 28, Pal.Black);
    }

    private static readonly Dictionary<string, Exercise> ById = All.ToDictionary(e => e.Id, StringComparer.Ordinal);

    public static bool Exists(string id) => ById.ContainsKey(id);

    /// <summary>The exercise, or <see cref="KeyNotFoundException"/>.</summary>
    public static Exercise Get(string id) =>
        ById.TryGetValue(id, out var e) ? e : throw new KeyNotFoundException("No exercise '" + id + "'.");

    /// <summary>An apple with no outline (fill, a light spot, stem and leaf), its box starting at (x, y), about 20×22.</summary>
    internal static void Apple(ArtCanvas c, int x, int y)
    {
        c.Ellipse(x, y + 3, x + 19, y + 21, Pal.Red);
        c.Ellipse(x + 3, y + 6, x + 7, y + 10, Pal.Orange);
        c.Rect(x + 9, y, x + 10, y + 3, Pal.Teal);
        c.Rows(x + 11, y, "..gg", ".ggg", "gg..");
    }
}
