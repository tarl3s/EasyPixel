using EasyPixel.Core.Model;

namespace EasyPixel.Core.Learn;

/// <summary>
/// Every practice exercise by id. A lesson's <c>[[exercise:id]]</c> must name one of these (tested). To add one, add an
/// entry to <see cref="All"/> (see AGENTS.md, "Lessons").
/// </summary>
public static class Exercises
{
    private static readonly Rgba Faint = Pal.Silver, Mark = Pal.Red;

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
        // 13 Tilesets: a 16×16 ground tile that repeats without a seam.
        new()
        {
            Id = "seamless-tile", Width = 16, Height = 16,
            Rules = new[] { Rules.FullyOpaque(), Rules.MinColors(3), Rules.MaxColors(6), Rules.Seamless() },
            Palette = new[] { Pal.Black, Pal.Ebony, Pal.Teal, Pal.Green, Pal.Lime, Pal.Plum, Pal.Red, Pal.Orange, Pal.Slate, Pal.Silver },
        },
    };

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
