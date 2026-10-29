using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Learn;

/// <summary>A lesson illustration: a sprite (several frames = it plays) and the zoom it is shown at.</summary>
public sealed record ArtExample(string Id, Sprite Sprite, int Zoom)
{
    public bool Animated => Sprite.Frames.Count > 1;
}

/// <summary>
/// The illustrations lessons show with <c>[[example:id]]</c>, drawn in code with the Core raster tools (crisp at any
/// integer zoom, MIT like the rest). To add one: <c>Add("id", zoom, w, h, c =&gt; …draw on the ArtCanvas…)</c> in
/// <see cref="Registry"/> (see AGENTS.md, "Lessons").
/// </summary>
public static partial class LessonArt
{
    private sealed record Entry(int Zoom, int Width, int Height, Action<ArtCanvas> Draw);

    private static readonly Lazy<Dictionary<string, Entry>> Registry = new(Build);

    public static IReadOnlyCollection<string> Ids => Registry.Value.Keys;

    public static bool Exists(string id) => Registry.Value.ContainsKey(id);

    /// <summary>Draws the illustration; <see cref="KeyNotFoundException"/> for an unknown id.</summary>
    public static ArtExample Build(string id)
    {
        if (!Registry.Value.TryGetValue(id, out var e)) throw new KeyNotFoundException("No lesson illustration '" + id + "'.");
        var c = new ArtCanvas(e.Width, e.Height);
        e.Draw(c);
        return new ArtExample(id, c.ToSprite(), e.Zoom);
    }

    private static Dictionary<string, Entry> Build()
    {
        var d = new Dictionary<string, Entry>(StringComparer.Ordinal);
        void Add(string id, int zoom, int w, int h, Action<ArtCanvas> draw) => d.Add(id, new Entry(zoom, w, h, draw));

        // ---- 01 Pixels and canvas size ----
        Add("potion-16", 10, 16, 16, c => Potion(c, 0, 0));
        Add("potion-1x", 1, 16, 16, c => Potion(c, 0, 0));
        Add("potion-2x", 2, 16, 16, c => Potion(c, 0, 0));
        Add("canvas-sizes", 5, 54, 34, c =>
        {
            c.Rect(0, 17, 17, 33, Pal.Silver, filled: false); // a 16×16 canvas
            Slime(c, 1, 18, 16, 15);
            c.Rect(20, 0, 53, 33, Pal.Silver, filled: false); // a 32×32 canvas
            Slime(c, 21, 1, 32, 32);
        });
        Add("mixels-bad", 7, 20, 20, c =>
        {
            // A tree drawn with 2×2 "pixels" for the leaves and 1×1 for the trunk: two pixel sizes clash.
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 8; x++)
                {
                    double dx = x - 3.5, dy = y - 3;
                    if (dx * dx / 16 + dy * dy / 9 > 1) continue;
                    var col = (x + y) % 3 == 0 ? Pal.Lime : dx + dy < 0 ? Pal.Green : Pal.Teal;
                    c.Rect(2 + x * 2, 1 + y * 2, 3 + x * 2, 2 + y * 2, col);
                }
            c.Rect(9, 13, 10, 18, Pal.Plum).Px(9, 14, Pal.Red).Px(10, 16, Pal.Black).Line(7, 19, 12, 19, Pal.Black);
        });
        Add("mixels-good", 7, 20, 20, c =>
        {
            c.Ellipse(2, 1, 17, 13, Pal.Teal).Ellipse(3, 1, 14, 10, Pal.Green).Ellipse(5, 2, 10, 6, Pal.Lime);
            c.Px(12, 9, Pal.Lime).Px(6, 9, Pal.Lime).Px(14, 5, Pal.Lime);
            c.Rect(9, 13, 10, 18, Pal.Plum).Px(9, 14, Pal.Red).Px(9, 15, Pal.Red).Line(7, 19, 12, 19, Pal.Black);
        });
        Add("orphans-bad", 8, 16, 16, c =>
        {
            Rock(c, 2, 4);
            foreach (var (x, y) in new[] { (5, 7), (9, 6), (7, 10), (11, 9), (4, 11), (10, 12), (8, 8), (12, 7) })
                c.Px(x, y, (x + y) % 2 == 0 ? Pal.White : Pal.Ebony);
        });
        Add("orphans-good", 8, 16, 16, c =>
        {
            Rock(c, 2, 4);
            c.Rows(4, 6, "ww", "w.").Rows(10, 11, "ee", ".e").Px(11, 10, Pal.Ebony);
        });

        // ---- 02 Lines and curves ----
        Add("jaggies-bad", 8, 24, 10, c =>
        {
            int x = 0, y = 9;
            foreach (int run in new[] { 1, 4, 1, 2, 5, 1, 3, 1, 4, 2 }) { c.Line(x, y, x + run - 1, y, Pal.Black); x += run; y--; }
        });
        Add("jaggies-good", 8, 24, 10, c =>
        {
            int x = 0, y = 9;
            for (int i = 0; i < 8; i++) { c.Line(x, y, x + 2, y, Pal.Black); x += 3; y--; }
        });
        Add("slopes", 6, 52, 13, c =>
        {
            c.Line(0, 12, 6, 6, Pal.Navy, false);         // 1:1
            c.Line(9, 12, 22, 6, Pal.Blue, false);        // 2:1
            for (int i = 0; i < 6; i++) c.Line(25 + i * 3, 12 - i, 27 + i * 3, 12 - i, Pal.Sky); // 3:1
            for (int i = 0; i < 6; i++) c.Line(45 + i, 12 - 2 * i, 45 + i, 11 - 2 * i, Pal.Red); // 1:2 (steep): two up, one across
        });
        Add("doubles-bad", 8, 14, 14, c =>
        {
            // A freehand line: every step leaves an L-shaped corner.
            for (int i = 0; i < 6; i++) { c.Px(1 + i * 2, 12 - i * 2, Pal.Black).Px(2 + i * 2, 12 - i * 2, Pal.Black).Px(2 + i * 2, 11 - i * 2, Pal.Black); }
        });
        Add("doubles-good", 8, 14, 14, c =>
        {
            for (int i = 0; i < 6; i++) c.Px(1 + i * 2, 12 - i * 2, Pal.Black).Px(2 + i * 2, 11 - i * 2, Pal.Black);
        });
        Add("curve-bad", 6, 24, 24, c => Arc(c, new[] { 3, 1, 4, 1, 2, 1, 1, 2 }));
        Add("curve-good", 6, 24, 24, c => Arc(c, new[] { 5, 3, 2, 1, 1, 1 }));

        // ---- 03 Shapes and silhouettes ----
        Add("silhouette-good", 6, 46, 24, c => { Knight(c, 0, 0, readable: true); c.Copy(0, 0, 22, 24, 24, 0); Blacken(c, 24, 46); });
        Add("silhouette-bad", 6, 46, 24, c => { Knight(c, 0, 0, readable: false); c.Copy(0, 0, 22, 24, 24, 0); Blacken(c, 24, 46); });
        Add("shape-language", 5, 56, 20, c =>
        {
            // Round = friendly, square = sturdy, triangle = danger.
            c.Ellipse(1, 4, 16, 19, Pal.Green).Ellipse(3, 6, 8, 10, Pal.Lime).Px(6, 11, Pal.Black).Px(11, 11, Pal.Black).Line(7, 15, 10, 15, Pal.Teal);
            c.Rect(20, 3, 35, 19, Pal.Slate).Rect(20, 3, 35, 5, Pal.Silver).Rect(23, 9, 25, 10, Pal.Yellow).Rect(30, 9, 32, 10, Pal.Yellow).Rect(24, 15, 31, 15, Pal.Ebony);
            for (int y = 0; y < 20; y++) { int half = y * 8 / 19; c.Line(47 - half, y, 47 + half, y, y < 6 ? Pal.Orange : Pal.Red); }
            c.Px(45, 12, Pal.Yellow).Px(49, 12, Pal.Yellow).Line(44, 16, 50, 16, Pal.Plum);
        });

        // ---- 05 Light and shading ----
        var orange = new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow };
        Add("pillow-shading", 6, 24, 24, c =>
        {
            c.Ellipse(1, 1, 22, 22, orange[0]).Ellipse(3, 3, 20, 20, orange[1]).Ellipse(6, 6, 17, 17, orange[2]).Ellipse(9, 9, 14, 14, orange[3]);
        });
        Add("light-source", 6, 24, 24, c =>
        {
            c.Sphere(1, 1, 22, 22, orange);
            c.Px(7, 6, Pal.White).Px(8, 6, Pal.White).Px(7, 7, Pal.White);
        });
        Add("cast-shadow", 5, 34, 28, c =>
        {
            c.Ellipse(8, 21, 33, 27, Pal.Ebony);
            c.Sphere(4, 2, 23, 21, orange);
            c.Px(9, 7, Pal.White).Px(10, 7, Pal.White);
        });
        Add("ramp-flat", 12, 24, 6, c =>
        {
            // Darker versions of one hue: muddy, lifeless.
            var basis = Pal.Orange;
            for (int i = 0; i < 4; i++) c.Rect(i * 6, 0, i * 6 + 5, 5, Pal.Mix(Pal.Hex("000000"), basis, 0.4 + i * 0.2));
        });
        Add("ramp-hue-shift", 12, 24, 6, c =>
        {
            var ramp = new[] { Pal.Navy, Pal.Red, Pal.Orange, Pal.Yellow };
            for (int i = 0; i < 4; i++) c.Rect(i * 6, 0, i * 6 + 5, 5, ramp[i]);
        });
        Add("cube-faces", 7, 24, 24, Cube);

        // ---- 06 Outlines ----
        Add("outline-none", 6, 30, 30, c => Exercises.Apple(c, 5, 4));
        Add("outline-black", 6, 30, 30, c => { Exercises.Apple(c, 5, 4); c.Outline(Pal.Black); });
        Add("outline-colored", 6, 30, 30, c => { Exercises.Apple(c, 5, 4); c.Outline(Pal.Plum); });
        Add("outline-selout", 6, 30, 30, c =>
        {
            Exercises.Apple(c, 5, 4);
            // Each outline pixel is a darker shade of what it touches, darkest on the side away from the light.
            c.SelOut((x, y, near) => near == Pal.Green || near == Pal.Lime ? Pal.Teal : near == Pal.Teal ? Pal.Black : x + y < 30 ? Pal.Plum : Pal.Navy);
        });
        Add("inner-lines", 6, 26, 26, c =>
        {
            // The outer outline is black; where the arm crosses the body a softer dark green line (an inner line) keeps them apart.
            c.Ellipse(4, 2, 19, 23, Pal.Green).Ellipse(6, 4, 10, 9, Pal.Lime).Rect(13, 7, 13, 9, Pal.Black);
            static bool Body(int x, int y) => Math.Pow((x - 12) / 8.0, 2) + Math.Pow((y - 13) / 11.0, 2) <= 1;
            static bool Arm(int x, int y) => Math.Pow((x - 16.5) / 6, 2) + Math.Pow((y - 15.5) / 2.6, 2) <= 1;
            for (int y = 0; y < 26; y++)
                for (int x = 0; x < 26; x++)
                {
                    if (!Arm(x, y)) continue;
                    bool edge = !Arm(x - 1, y) || !Arm(x + 1, y) || !Arm(x, y - 1) || !Arm(x, y + 1);
                    c.Px(x, y, edge && Body(x, y) ? Pal.Teal : Pal.Green);
                }
            c.Outline(Pal.Black);
        });

        // ---- 08 Dithering ----
        Add("dither-checker", 8, 24, 8, c =>
        {
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 24; x++)
                    c.Px(x, y, x < 8 ? Pal.Navy : x >= 16 ? Pal.Blue : (x + y) % 2 == 0 ? Pal.Navy : Pal.Blue);
        });
        Add("dither-gradient", 6, 40, 10, c =>
        {
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 40; x++)
                    c.Px(x, y, Dither.Pattern(DitherKind.Bayer4, x, y, x / 39.0) ? Pal.Orange : Pal.Plum);
        });

        // ---- 10/11 Animation ----
        Add("idle-bob", 8, 16, 16, c =>
        {
            Slime(c, 1, 3, 14, 13);
            c.Duration(300).NewFrame(durationMs: 300);
            Slime(c, 0, 5, 16, 11);
        });
        Add("walk-4frame", 8, 14, 21, c =>
        {
            for (int f = 0; f < 4; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 150); else c.Duration(150);
                Walker(c, f, 0, 1); // one row of headroom: the body rises on the passing frames
            }
        });

        // ---- 13 Tilesets ----
        Add("tile-seam-bad", 4, 48, 32, c => { for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) BadTile(c, i * 16, j * 16); });
        Add("tile-seam-good", 4, 48, 32, c => { for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) GrassTile(c, i * 16, j * 16, standout: false); });
        Add("tile-repeat-bad", 4, 48, 32, c => { for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) GrassTile(c, i * 16, j * 16, standout: true); });
        Add("tile-single", 10, 16, 16, c => GrassTile(c, 0, 0, standout: false));
        Add("tileset-edges", 5, 76, 30, c =>
        {
            // The 3×3 set (corners, edges, middle) with gaps between the tiles, and a platform built from it.
            for (int ty = 0; ty < 3; ty++) for (int tx = 0; tx < 3; tx++) GroundTile(c, tx * 10, ty * 10, tx, ty);
            for (int i = 0; i < 5; i++)
            {
                int col = i == 0 ? 0 : i == 4 ? 2 : 1;
                GroundTile(c, 36 + i * 8, 7, col, 0);
                GroundTile(c, 36 + i * 8, 15, col, 2);
            }
        });

        // ---- 14 Backgrounds and parallax: far moves least ----
        Add("parallax-layers", 4, 64, 32, c =>
        {
            for (int f = 0; f < 16; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 80); else c.Duration(80);
                Parallax(c, f);
            }
        });

        More(Add);
        return d;
    }

    // ---- drawing helpers ----

    private static void Potion(ArtCanvas c, int x, int y) => c.Rows(x, y,
        "................",
        "......kkkk......",
        "......kyok......",
        "......kkkk......",
        "......kwsk......",
        ".....kkwskk.....",
        "....kwwssssk....",
        "...kwooorrrrk...",
        "..kwoorrrrrrpk..",
        "..kworrrrrrrpk..",
        "..krrrrrrrrppk..",
        "..krrrrrrrpppk..",
        "...krrrrrpppk...",
        "....kpppppppk...",
        ".....kkkkkk.....",
        "................");

    /// <summary>A green slime in the box (x, y, w, h): a round dome with a dark rim, a highlight, eyes and an outline inside the box.</summary>
    internal static void Slime(ArtCanvas c, int x, int y, int w, int h)
    {
        bool In(int px, int py)
        {
            if (px < x || px >= x + w || py < y || py >= y + h) return false;
            double dx = (px + 0.5 - (x + w / 2.0)) / (w / 2.0), dy = (py + 0.5 - (y + h)) / h;
            return Math.Pow(Math.Abs(dx), 2.4) + Math.Pow(Math.Abs(dy), 2.4) <= 1;
        }
        double hx = x + w * 0.32, hy = y + h * 0.38, hr = Math.Max(1, w * 0.12);
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
            {
                if (!In(px, py)) continue;
                bool edge = !In(px - 1, py) || !In(px + 1, py) || !In(px, py - 1) || !In(px, py + 1);
                double d = Math.Sqrt((px + 0.5 - hx) * (px + 0.5 - hx) + (py + 0.5 - hy) * (py + 0.5 - hy));
                c.Px(px, py, edge ? Pal.Black : py >= y + h - 1 - Math.Max(1, h / 8) ? Pal.Teal : d <= hr ? Pal.Lime : Pal.Green);
            }
        int eye = Math.Max(1, w / 10);
        c.Rect(x + w * 2 / 5, y + h / 2, x + w * 2 / 5 + eye - 1, y + h / 2 + eye, Pal.Black);
        c.Rect(x + w * 3 / 4 - eye, y + h / 2, x + w * 3 / 4 - 1, y + h / 2 + eye, Pal.Black);
    }

    /// <summary>An isometric cube lit from above-left: top lightest, left face middle, right face darkest.</summary>
    private static void Cube(ArtCanvas c)
    {
        for (int x = 0; x < 22; x++)
        {
            int d = x <= 10 ? 10 - x : x - 11;  // 0 at the middle columns, 10 at the sides
            int half = (10 - d) / 2;
            int bottom = 5 + half;             // the lower edge of the top face in this column
            c.Line(x + 1, 6 - half, x + 1, bottom + 1, Pal.Yellow);
            c.Line(x + 1, bottom + 2, x + 1, bottom + 12, x <= 10 ? Pal.Orange : Pal.Red);
        }
        c.Outline(Pal.Black);
    }

    private static void Rock(ArtCanvas c, int x, int y)
    {
        c.Ellipse(x, y, x + 11, y + 9, Pal.Slate);
        c.Ellipse(x + 1, y, x + 7, y + 5, Pal.Silver);
        c.Shade((px, py, col) => col == Pal.Slate && px + py > x + y + 13 ? Pal.Ebony : null);
    }

    /// <summary>A quarter curve from the bottom-left up to the top-right, one horizontal run per row (lengths given).</summary>
    private static void Arc(ArtCanvas c, int[] runs)
    {
        int x = 0, y = c.Height - 1;
        foreach (int run in runs)
        {
            c.Line(x, y, x + run - 1, y, Pal.Black);
            x += run;
            y--;
        }
        // then mirror: vertical runs going up
        for (int i = runs.Length - 1; i >= 0 && y >= 0; i--)
        {
            int run = runs[i];
            c.Line(x, y, x, Math.Max(0, y - run + 1), Pal.Black);
            y -= run;
            x++;
        }
    }

    /// <summary>A small knight. Readable: limbs apart, sword held out, plume and shield sticking out. Not: everything pressed together.</summary>
    private static void Knight(ArtCanvas c, int x, int y, bool readable)
    {
        if (readable)
        {
            c.Rect(x + 8, y, x + 9, y + 2, Pal.Red).Px(x + 10, y + 1, Pal.Red).Px(x + 7, y + 1, Pal.Red);
            c.Ellipse(x + 6, y + 3, x + 11, y + 8, Pal.Silver).Line(x + 8, y + 5, x + 11, y + 5, Pal.Black);
            c.Rect(x + 6, y + 9, x + 11, y + 15, Pal.Blue).Line(x + 6, y + 14, x + 11, y + 14, Pal.Yellow);
            c.Rect(x + 6, y + 16, x + 7, y + 20, Pal.Slate).Rect(x + 10, y + 16, x + 11, y + 20, Pal.Slate);
            c.Line(x + 4, y + 21, x + 7, y + 21, Pal.Ebony).Line(x + 10, y + 21, x + 13, y + 21, Pal.Ebony);
            c.Rect(x + 12, y + 10, x + 14, y + 11, Pal.Silver);
            c.Line(x + 15, y + 9, x + 20, y + 4, Pal.White).Line(x + 14, y + 8, x + 16, y + 10, Pal.Yellow);
            c.Rect(x + 1, y + 9, x + 4, y + 15, Pal.Red).Rect(x + 2, y + 11, x + 3, y + 13, Pal.Yellow);
        }
        else
        {
            c.Ellipse(x + 6, y + 3, x + 11, y + 8, Pal.Silver).Line(x + 8, y + 5, x + 11, y + 5, Pal.Black);
            c.Rect(x + 5, y + 9, x + 12, y + 16, Pal.Blue).Line(x + 5, y + 14, x + 12, y + 14, Pal.Yellow);
            c.Rect(x + 6, y + 9, x + 9, y + 15, Pal.Red).Rect(x + 7, y + 11, x + 8, y + 13, Pal.Yellow);
            c.Rect(x + 6, y + 17, x + 11, y + 20, Pal.Slate).Line(x + 6, y + 21, x + 11, y + 21, Pal.Ebony);
        }
        c.Outline(Pal.Black);
    }

    /// <summary>Paints everything drawn between columns x0 and x1 black (a silhouette).</summary>
    private static void Blacken(ArtCanvas c, int x0, int x1) => c.Shade((x, y, col) => x >= x0 && x < x1 ? Pal.Black : null);

    /// <summary>A tiny walker facing right, frame 0–3: contact, passing (body up a pixel), contact with the legs swapped, passing.</summary>
    internal static void Walker(ArtCanvas c, int f, int ox = 0, int oy = 0)
    {
        int[] front = { 9, 6, 2, 5 }, back = { 2, 5, 9, 6 }, arm = { 3, 6, 9, 6 };
        WalkerPose(c, ox, oy, front[f], back[f], arm[f], f % 2 == 1 ? -1 : 0);
        c.Outline(Pal.Black);
    }

    /// <summary>
    /// The walker's body at (ox, oy) without its outline: the near and far feet at x = <paramref name="front"/> and
    /// <paramref name="back"/> (on row 18), the hand at x = <paramref name="arm"/>, the body raised by -<paramref name="b"/>.
    /// </summary>
    internal static void WalkerPose(ArtCanvas c, int ox, int oy, int front, int back, int arm, int b)
    {
        int y = oy + b;
        c.Line(ox + 7, y + 13, ox + back, oy + 18, Pal.Ebony).Line(ox + 8, y + 13, ox + back + 1, oy + 18, Pal.Ebony);   // the far leg, darker
        c.Rect(ox + 4, y + 7, ox + 9, y + 12, Pal.Blue);
        c.Line(ox + 5, y + 13, ox + front, oy + 18, Pal.Navy).Line(ox + 6, y + 13, ox + front + 1, oy + 18, Pal.Navy);  // the near leg
        c.Ellipse(ox + 4, y + 1, ox + 9, y + 6, Pal.Orange).Line(ox + 5, y + 1, ox + 8, y + 1, Pal.Plum).Line(ox + 4, y + 2, ox + 7, y + 2, Pal.Plum).Px(ox + 8, y + 4, Pal.Black);
        c.Line(ox + 6, y + 8, ox + arm, y + 11, Pal.Sky).Px(ox + arm, y + 12, Pal.Orange);
    }

    /// <summary>A seamless grass tile at (x, y): every detail is placed with wrap-around, so copies line up.</summary>
    private static void GrassTile(ArtCanvas c, int x, int y, bool standout)
    {
        c.Rect(x, y, x + 15, y + 15, Pal.Green);
        void Wrap(int px, int py, Rgba col) => c.Px(x + ((px % 16) + 16) % 16, y + ((py % 16) + 16) % 16, col);
        foreach (var (px, py) in new[] { (2, 3), (9, 1), (13, 7), (5, 10), (11, 13), (15, 11), (0, 15), (7, 6) })
        {
            // a little "v" of grass with a dark speck under it
            Wrap(px, py, Pal.Lime);
            Wrap(px + 2, py, Pal.Lime);
            Wrap(px + 1, py + 1, Pal.Lime);
            Wrap(px + 1, py + 2, Pal.Teal);
        }
        if (standout) c.Ellipse(x + 5, y + 5, x + 10, y + 9, Pal.Silver).Ellipse(x + 6, y + 5, x + 8, y + 6, Pal.White).Line(x + 6, y + 9, x + 10, y + 9, Pal.Slate);
    }

    /// <summary>A grass tile that doesn't repeat: light on the left, dark on the right, a tuft cut by the edge.</summary>
    private static void BadTile(ArtCanvas c, int x, int y)
    {
        for (int i = 0; i < 16; i++)
            c.Line(x + i, y, x + i, y + 15, i < 5 ? Pal.Lime : i > 11 ? Pal.Teal : Pal.Green);
        c.Rect(x + 13, y + 4, x + 15, y + 6, Pal.Yellow);
        c.Line(x, y + 15, x + 15, y + 15, Pal.Ebony);
    }

    /// <summary>One 8×8 ground tile of a 3×3 set (col, row 0–2): grass and a rounded top on row 0, outlines on the outer sides.</summary>
    private static void GroundTile(ArtCanvas c, int x, int y, int col, int row)
    {
        const int w = 8;
        c.Rect(x, y, x + w - 1, y + 7, Pal.Plum);
        for (int i = 0; i < w; i += 3) c.Px(x + i, y + 4 + (i % 2), Pal.Red);
        if (row == 0) c.Rect(x, y, x + w - 1, y + 2, Pal.Green).Line(x, y, x + w - 1, y, Pal.Lime);
        if (row == 2) c.Line(x, y + 7, x + w - 1, y + 7, Pal.Black);
        if (col == 0) c.Line(x, y, x, y + 7, Pal.Black);
        if (col == 2) c.Line(x + w - 1, y, x + w - 1, y + 7, Pal.Black);
        if (row == 0) c.Line(x, y, x + w - 1, y, Pal.Black).Line(x, y + 1, x + w - 1, y + 1, Pal.Lime);
        if (row == 0 && col == 0) c.Px(x, y, Rgba.Transparent).Px(x + 1, y, Rgba.Transparent).Px(x, y + 1, Rgba.Transparent).Px(x + 1, y + 1, Pal.Black);
        if (row == 0 && col == 2) c.Px(x + w - 1, y, Rgba.Transparent).Px(x + w - 2, y, Rgba.Transparent).Px(x + w - 1, y + 1, Rgba.Transparent).Px(x + w - 2, y + 1, Pal.Black);
        if (row == 2 && col == 0) c.Px(x, y + 7, Rgba.Transparent).Px(x + 1, y + 6, Pal.Black).Px(x, y + 6, Rgba.Transparent);
        if (row == 2 && col == 2) c.Px(x + w - 1, y + 7, Rgba.Transparent).Px(x + w - 2, y + 6, Pal.Black).Px(x + w - 1, y + 6, Rgba.Transparent);
    }

    /// <summary>One frame of the parallax scene: sky and far mountains still, hills move 1 px a frame, the near ground 2.</summary>
    private static void Parallax(ArtCanvas c, int f)
    {
        c.Rect(0, 0, 63, 31, Pal.Aqua).Rect(0, 0, 63, 9, Pal.Sky);
        for (int x = 0; x < 64; x++)
        {
            int far = 14 + (int)Math.Round(4 * Math.Abs(Math.Sin(x / 9.0)) + 2 * Math.Sin(x / 3.7));
            c.Line(x, far, x, 31, Pal.Silver);
            int hx = x + f;
            int hill = 20 + (int)Math.Round(3 * Math.Sin(hx * Math.PI / 8));
            c.Line(x, hill, x, 31, Pal.Teal);
            int nx = (x + f * 2) % 32;
            c.Line(x, 27, x, 31, Pal.Green);
            c.Px(x, 27, Pal.Lime);
            if (nx is >= 6 and <= 8) c.Line(x, 20, x, 26, Pal.Plum);
            if (nx is >= 3 and <= 11) for (int y = 12; y <= 21; y++) if ((nx - 7) * (nx - 7) + (y - 16.5) * (y - 16.5) <= 18) c.Px(x, y, Pal.Ebony);
        }
    }
}
