using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Learn;

// The illustrations of lessons 04 and 07–18 (palettes, anti-aliasing, dithering, materials, animation, walks,
// characters, backgrounds, the school, icons, effects, game-ready sheets).
public static partial class LessonArt
{
    private static void More(Action<string, int, int, int, Action<ArtCanvas>> Add)
    {
        // ---- 04 Colour palettes ----
        Add("palette-sizes", 8, 32, 11, c =>
        {
            var four = new[] { Pal.Hex("0f380f"), Pal.Hex("306230"), Pal.Hex("8bac0f"), Pal.Hex("cadc9f") };
            for (int i = 0; i < 4; i++) c.Rect(i * 8, 0, i * 8 + 7, 2, four[i]);
            var eight = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.Green, Pal.Blue, Pal.White };
            for (int i = 0; i < 8; i++) c.Rect(i * 4, 4, i * 4 + 3, 6, eight[i]);
            var sixteen = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.Lime, Pal.Green, Pal.Teal, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Aqua, Pal.White, Pal.Silver, Pal.Slate, Pal.Ebony };
            for (int i = 0; i < 16; i++) c.Rect(i * 2, 8, i * 2 + 1, 10, sixteen[i]);
        });
        Add("ramps-shared", 8, 34, 22, c =>
        {
            // Three hue-shifted ramps that share their ends: a 13-colour palette that covers skin, plants and water.
            var rows = new[]
            {
                new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow, Pal.White },
                new[] { Pal.Navy, Pal.Teal, Pal.Green, Pal.Lime, Pal.Yellow },
                new[] { Pal.Black, Pal.Navy, Pal.Blue, Pal.Sky, Pal.Aqua },
            };
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < 5; i++) c.Rect(i * 7, r * 8, i * 7 + 5, r * 8 + 5, rows[r][i]);
        });
        // The same mushroom: colourful but with one value (a grey blob in greyscale) vs. planned values.
        Add("value-bad", 6, 38, 18, c =>
        {
            Mushroom(c, 0, 0, Pal.Hex("8a4a6a"), Pal.Hex("d04050"), Pal.Hex("e06a5a"), Pal.Hex("3c9a50"), Pal.Hex("a07860"), Pal.Hex("8a6a6a"));
            c.Copy(0, 0, 18, 18, 20, 0);
            c.Shade((x, _, col) => x >= 20 ? Grey(col) : null);
        });
        Add("value-good", 6, 38, 18, c =>
        {
            Mushroom(c, 0, 0, Pal.Plum, Pal.Red, Pal.Orange, Pal.White, Pal.Yellow, Pal.Orange);
            c.Outline(Pal.Black);
            c.Copy(0, 0, 18, 18, 20, 0);
            c.Shade((x, _, col) => x >= 20 ? Grey(col) : null);
        });

        // ---- 07 Anti-aliasing ----
        Add("aa-none", 8, 24, 24, c => AaShape(c, 0));
        Add("aa-good", 8, 24, 24, c => AaShape(c, 1));
        Add("aa-overdone", 8, 24, 24, c => AaShape(c, 2));
        Add("aa-halo", 6, 40, 20, c =>
        {
            // Left: the coin was anti-aliased against a white page, so a light fringe glows on the dark game background.
            for (int i = 0; i < 2; i++)
            {
                int x = i * 20;
                c.Ellipse(x + 3, 3, x + 16, 16, Pal.Yellow);
                c.Shade((px, py, col) => px >= x && px < x + 20 && col == Pal.Yellow && px + py > x + 22 ? Pal.Orange : null);
                c.Px(x + 7, 6, Pal.White).Px(x + 6, 7, Pal.White);
            }
            c.Outline(Pal.Black);
            var src = c.Image.Clone();
            for (int y = 0; y < 20; y++)
                for (int x = 0; x < 20; x++)
                {
                    if ((src.Get(x, y) >> 24) != 0) continue;
                    bool near = (src.Get(x - 1, y) >> 24) != 0 || (src.Get(x + 1, y) >> 24) != 0 || (src.Get(x, y - 1) >> 24) != 0 || (src.Get(x, y + 1) >> 24) != 0;
                    if (near) c.Px(x, y, Pal.Silver);
                }
            for (int y = 0; y < 20; y++)
                for (int x = 0; x < 40; x++)
                    if (!c.IsSet(x, y)) c.Px(x, y, Pal.Navy);
        });

        // ---- 08 Dithering ----
        Add("dither-banding", 6, 40, 10, c => Bands(c, dither: false));
        Add("dither-smooth", 6, 40, 10, c => Bands(c, dither: true));
        Add("dither-ball-good", 6, 24, 24, c => DitherBall(c, noisy: false));
        Add("dither-ball-noisy", 6, 24, 24, c => DitherBall(c, noisy: true));

        // ---- 09 Textures and materials ----
        Add("materials", 4, 88, 16, c =>
        {
            Metal(c, 0, 0);
            Wood(c, 18, 0);
            Stone(c, 36, 0);
            Cloth(c, 54, 0);
            GrassTile(c, 72, 0, standout: false);
        });
        Add("texture-noise", 8, 16, 16, c =>
        {
            c.Rect(0, 0, 15, 15, Pal.Slate);
            var specks = new[] { Pal.Silver, Pal.Ebony, Pal.White, Pal.Black };
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int h = Hash(x, y);
                    if (h % 100 < 40) c.Px(x, y, specks[h / 100 % 4]);
                }
        });
        Add("texture-clusters", 8, 16, 16, c => Stone(c, 0, 0));

        // ---- 10 Animation basics ----
        Add("bounce-ball", 6, 24, 32, c =>
        {
            // (bottom row of the ball, width, height, ms): squash on the floor, stretch when fast, slow at the top.
            var keys = new[] { (30, 12, 7, 120), (24, 8, 11, 60), (16, 9, 9, 70), (11, 9, 9, 100), (9, 9, 9, 160), (11, 9, 9, 100), (16, 9, 9, 70), (24, 8, 11, 60) };
            for (int f = 0; f < keys.Length; f++)
            {
                var (bottom, w, h, ms) = keys[f];
                if (f > 0) c.NewFrame(durationMs: ms); else c.Duration(ms);
                Ball(c, 12, bottom, w, h);
                c.Outline(Pal.Black);
                c.Line(0, 31, 23, 31, Pal.Teal);
            }
        });
        Add("bounce-spacing", 6, 56, 32, c =>
        {
            // One bounce, a frame every 6 px across: the drawings bunch up at the top (slow) and spread near the floor (fast).
            for (int i = 0; i <= 8; i++)
            {
                double t = i / 8.0;
                int y = (int)Math.Round(26 - 22 * 4 * t * (1 - t));
                c.Ellipse(2 + i * 6, y - 4, 6 + i * 6, y, i is 0 or 8 ? Pal.Orange : Pal.Mix(Pal.Orange, Pal.White, 0.45));
            }
            c.Outline(Pal.Black);
            c.Line(0, 28, 55, 28, Pal.Teal);
        });
        Add("squash-stretch", 6, 44, 18, c =>
        {
            Ball(c, 6, 16, 11, 11);    // at rest
            Ball(c, 21, 16, 15, 7);    // squashed on landing: wider and flatter
            Ball(c, 37, 16, 8, 15);    // stretched while falling fast: taller and thinner
            c.Outline(Pal.Black);
        });
        Add("onion-skin", 8, 18, 18, c =>
        {
            Ghost(c, 18, 18, g => Slime(g, 4, 0, 10, 15), Pal.Mix(Pal.Red, Pal.White, 0.55));   // the frame before
            Ghost(c, 18, 18, g => Slime(g, 0, 9, 18, 9), Pal.Mix(Pal.Sky, Pal.White, 0.45));  // the frame after
            Slime(c, 2, 3, 14, 13);                                                                // the frame being drawn
        });

        // ---- 11 Walk cycle ----
        Add("walk-6frame", 8, 14, 21, c =>
        {
            // Contact, down, passing, then the same with the legs swapped; the body is lowest on "down" and highest on "passing".
            int[] front = { 10, 8, 6, 2, 3, 5 }, back = { 2, 3, 5, 10, 8, 6 }, arm = { 3, 5, 6, 9, 7, 6 }, bob = { 0, 1, -1, 0, 1, -1 };
            for (int f = 0; f < 6; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 120); else c.Duration(120);
                WalkerPose(c, 0, 1, front[f], back[f], arm[f], bob[f]);
                c.Outline(Pal.Black);
            }
        });
        Add("walk-poses", 7, 48, 22, c =>
        {
            WalkerPose(c, 1, 1, 10, 2, 3, 0);    // contact: legs furthest apart, heels on the ground
            WalkerPose(c, 17, 1, 8, 3, 5, 1);    // down: the weight lands, the body sinks
            WalkerPose(c, 33, 1, 6, 5, 6, -1);   // passing: one leg under the body, the body at its highest
            c.Outline(Pal.Black);
            c.Line(0, 21, 47, 21, Pal.Teal);
            for (int x = 0; x < 48; x += 2) if (c.Image.Get(x, 1) >> 24 == 0) c.Px(x, 1, Pal.Sky); // the top of the contact pose's head (the passing head pokes above it)
        });

        // ---- 12 Character sprites ----
        Add("proportions", 6, 40, 34, c =>
        {
            Hero(c, 1, 16, HeroFront);
            Tall(c, 24, 1);
            c.Outline(Pal.Black);
        });
        Add("facing-dirs", 7, 40, 17, c =>
        {
            Hero(c, 1, 1, HeroFront);
            Hero(c, 14, 1, HeroSide);
            Hero(c, 27, 1, HeroBack);
            c.Outline(Pal.Black);
        });
        Add("hero-silhouette", 7, 26, 17, c =>
        {
            Hero(c, 1, 1, HeroSide);
            c.Outline(Pal.Black);
            c.Copy(0, 0, 13, 17, 13, 0);
            c.Shade((x, _, _) => x >= 13 ? Pal.Black : null);
        });

        // ---- 14 Backgrounds and parallax ----
        Add("atmosphere-bad", 4, 64, 32, c => Ranges(c, depth: false));
        Add("atmosphere-good", 4, 64, 32, c => Ranges(c, depth: true));
        Add("hills-strip", 4, 64, 24, c => Hills(c, x => 10 + 4 * Math.Sin(2 * Math.PI * x / 64) + 2 * Math.Sin(2 * Math.PI * 3 * x / 64)));
        Add("hills-strip-bad", 4, 64, 24, c => Hills(c, x => 6 + 10 * x / 63.0 + 2 * Math.Sin(x / 5.0)));

        // ---- 15 Buildings and props: the school ----
        Add("school-side", 4, 64, 48, SchoolSide);
        Add("school-34", 4, 64, 48, School34);
        Add("school-top", 4, 64, 48, SchoolTop);
        Add("windows-good", 6, 44, 12, c => WindowRow(c, new[] { (2, 2, 6, 8), (13, 2, 6, 8), (24, 2, 6, 8), (35, 2, 6, 8) }));
        Add("windows-bad", 6, 44, 12, c => WindowRow(c, new[] { (2, 2, 6, 8), (13, 3, 5, 7), (24, 1, 7, 8), (36, 2, 6, 9) }));
        Add("school-props", 5, 60, 22, SchoolProps);

        // ---- 16 Items and icons ----
        Add("icon-set", 4, 88, 16, IconSet);
        Add("icon-set-1x", 1, 88, 16, IconSet);
        Add("icon-detail-bad", 8, 16, 16, c => Sword(c, 0, 0, good: false));
        Add("icon-detail-good", 8, 16, 16, c => { Sword(c, 0, 0, good: true); c.Outline(Pal.Black); });

        // ---- 17 Effects ----
        Add("fx-fire", 8, 16, 24, c =>
        {
            for (int f = 0; f < 4; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 100); else c.Duration(100);
                Flame(c, f);
            }
        });
        Add("fx-smoke", 6, 20, 26, c =>
        {
            for (int f = 0; f < 6; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 110); else c.Duration(110);
                Smoke(c, f);
            }
        });
        Add("fx-explosion", 6, 32, 32, c =>
        {
            for (int f = 0; f < 7; f++)
            {
                if (f > 0) c.NewFrame(durationMs: f == 0 ? 50 : 70); else c.Duration(50);
                Explosion(c, f);
            }
        });
        Add("fx-sparkle", 8, 15, 15, c =>
        {
            int[] arms = { 1, 3, 6, 2 };
            for (int f = 0; f < 4; f++)
            {
                if (f > 0) c.NewFrame(durationMs: 90); else c.Duration(90);
                Sparkle(c, 7, 7, arms[f], diagonal: f == 2);
            }
        });

        // ---- 18 Making it game-ready ----
        Add("sheet-layout", 4, 61, 22, c =>
        {
            for (int f = 0; f < 4; f++) WalkerPose(c, f * 15 + 1, 1, new[] { 9, 6, 2, 5 }[f], new[] { 2, 5, 9, 6 }[f], new[] { 3, 6, 9, 6 }[f], f % 2 == 1 ? -1 : 0);
            c.Outline(Pal.Black);
            for (int i = 0; i <= 4; i++) c.Line(i * 15, 0, i * 15, 21, Pal.Silver);
            c.Line(0, 0, 60, 0, Pal.Silver).Line(0, 21, 60, 21, Pal.Silver);
        });
        Add("sheet-tags", 4, 96, 26, c =>
        {
            Slime(c, 1, 9, 14, 13);
            Slime(c, 17, 11, 14, 11);
            for (int f = 0; f < 4; f++) WalkerPose(c, 33 + f * 16, 3, new[] { 9, 6, 2, 5 }[f], new[] { 2, 5, 9, 6 }[f], new[] { 3, 6, 9, 6 }[f], f % 2 == 1 ? -1 : 0);
            c.Outline(Pal.Black);
            c.Rect(0, 0, 30, 1, Pal.Green);   // the "idle" tag over frames 1–2
            c.Rect(32, 0, 95, 1, Pal.Blue);   // the "walk" tag over frames 3–6
        });
        Add("frames-aligned-good", 8, 16, 22, c => AlignedWalk(c, new[] { (1, 1), (1, 1), (1, 1), (1, 1) }));
        Add("frames-aligned-bad", 8, 16, 22, c => AlignedWalk(c, new[] { (1, 1), (0, 2), (2, 0), (1, 2) }));
    }

    // ---- drawing helpers (lessons 04 and 07–18) ----

    /// <summary>A small deterministic hash for "random" texture.</summary>
    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7fffffff;
        }
    }

    private static Rgba Grey(Rgba c)
    {
        byte l = (byte)Math.Round(Pal.Luma(c));
        return new Rgba(l, l, l, 255);
    }

    /// <summary>A mushroom in an 18×18 box at (x, y): a cap with a darker underside, a light spot and dots, a stem.</summary>
    private static void Mushroom(ArtCanvas c, int x, int y, Rgba capDark, Rgba cap, Rgba capLight, Rgba spot, Rgba stem, Rgba stemDark)
    {
        c.Rect(x + 6, y + 9, x + 11, y + 16, stem).Line(x + 10, y + 11, x + 10, y + 16, stemDark).Line(x + 11, y + 10, x + 11, y + 16, stemDark);
        c.Ellipse(x + 1, y + 1, x + 16, y + 11, cap);
        c.Shade((px, py, col) => px >= x && px < x + 18 && py >= y + 8 && py <= y + 11 && col == cap ? capDark : null);
        c.Ellipse(x + 4, y + 2, x + 8, y + 4, capLight);
        c.Rect(x + 10, y + 3, x + 11, y + 4, spot).Rect(x + 5, y + 6, x + 6, y + 7, spot).Px(x + 13, y + 6, spot).Px(x + 14, y + 6, spot);
    }

    /// <summary>A quarter-pie outline on white (two straight sides and a curve): 0 = no AA, 1 = AA on the curve's steps, 2 = AA everywhere.</summary>
    private static void AaShape(ArtCanvas c, int mode)
    {
        c.Rect(0, 0, 23, 23, Pal.White);
        c.Arc(21, 21, 17, 17, 180, 270, Pal.Black);
        c.Line(4, 21, 21, 21, Pal.Black).Line(21, 4, 21, 21, Pal.Black);
        if (mode == 1) AntiAlias(c, Pal.Black, 21, 21, 17, Pal.Slate, Pal.Silver);
        if (mode == 2)
        {
            var src = c.Image.Clone();
            uint k = Pal.Black.Packed;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                    if (src.Get(x, y) != k && (src.Get(x - 1, y) == k || src.Get(x + 1, y) == k || src.Get(x, y - 1) == k || src.Get(x, y + 1) == k))
                        c.Px(x, y, Pal.Silver);
        }
    }

    /// <summary>
    /// Manual anti-aliasing of a circle arc of radius <paramref name="r"/> around (cx, cy): at each step of the line,
    /// the corner pixel nearest the true curve gets <paramref name="near"/> (almost on it) or <paramref name="far"/>.
/// Joins inside a 1:1 diagonal (one-pixel steps) are left alone, as lesson 07 teaches.
    /// </summary>
    public static void AntiAlias(ArtCanvas c, Rgba line, double cx, double cy, double r, Rgba near, Rgba far)
    {
        var src = c.Image.Clone();
        bool IsLine(int x, int y) => src.Get(x, y) == line.Packed;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (!IsLine(x, y)) continue;
                foreach (int dx in new[] { -1, 1 })
                    foreach (int dy in new[] { -1, 1 })
                    {
                        if (!IsLine(x + dx, y + dy) || IsLine(x + dx, y) || IsLine(x, y + dy)) continue;
                        if (IsLine(x - dx, y - dy) && IsLine(x + 2 * dx, y + 2 * dy)) continue; // inside a 1:1 diagonal: left alone
                        double Off(int px, int py) => Math.Abs(Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) - r);
                        var (px, py) = Off(x + dx, y) < Off(x, y + dy) ? (x + dx, y) : (x, y + dy);
                        c.Px(px, py, Off(px, py) < 0.75 ? near : far);
                    }
            }
    }

    /// <summary>Plum → red → orange → yellow across the width: four hard bands, or blended with an ordered dither.</summary>
    private static void Bands(ArtCanvas c, bool dither)
    {
        var ramp = new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow };
        for (int x = 0; x < c.Width; x++)
            for (int y = 0; y < c.Height; y++)
            {
                if (!dither) { c.Px(x, y, ramp[Math.Min(3, x * 4 / c.Width)]); continue; }
                double t = x / (c.Width - 1.0) * 3;
                int i = Math.Min(2, (int)t);
                c.Px(x, y, Dither.Pattern(DitherKind.Bayer4, x, y, t - i) ? ramp[i + 1] : ramp[i]);
            }
    }

    /// <summary>A shaded ball: dithered only where two shades meet, or speckled with noise all over.</summary>
    private static void DitherBall(ArtCanvas c, bool noisy)
    {
        var ramp = new[] { Pal.Navy, Pal.Blue, Pal.Sky, Pal.Aqua };
        double cx = 12, cy = 12, rr = 10.5;
        double len = Math.Sqrt(3);
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                double nx = (x + 0.5 - cx) / rr, ny = (y + 0.5 - cy) / rr, d = nx * nx + ny * ny;
                if (d > 1) continue;
                double light = Math.Max(0, (-nx - ny + Math.Sqrt(1 - d)) / len) * 3.6;
                int i = Math.Clamp((int)light, 0, 3);
                double f = light - i;
                Rgba col;
                if (noisy) col = ramp[Math.Clamp(i + (Hash(x, y) % 100 < 45 ? (Hash(y, x) % 2 == 0 ? 1 : -1) : 0), 0, 3)];
                else col = i < 3 && f > 0.6 && Dither.Pattern(DitherKind.Checker, x, y, 0.5) ? ramp[i + 1] : ramp[i];
                c.Px(x, y, col);
            }
        c.Outline(Pal.Black);
    }

    private static void Metal(ArtCanvas c, int x, int y)
    {
        c.Rect(x, y, x + 15, y + 15, Pal.Slate);
        c.Rect(x, y, x + 15, y + 1, Pal.Silver).Line(x, y, x, y + 15, Pal.Silver);
        c.Rect(x, y + 14, x + 15, y + 15, Pal.Ebony).Line(x + 15, y + 1, x + 15, y + 15, Pal.Ebony);
        c.Line(x + 3, y + 12, x + 10, y + 5, Pal.Silver).Line(x + 4, y + 12, x + 11, y + 5, Pal.White).Line(x + 7, y + 12, x + 11, y + 8, Pal.Silver);
        foreach (var (rx, ry) in new[] { (3, 3), (12, 3), (3, 11), (12, 11) })
            c.Px(x + rx, y + ry, Pal.White).Px(x + rx + 1, y + ry + 1, Pal.Ebony);
    }

    private static void Wood(ArtCanvas c, int x, int y)
    {
        for (int p = 0; p < 4; p++)
        {
            int top = y + p * 4;
            c.Rect(x, top, x + 15, top + 3, Pal.Red);
            c.Line(x, top, x + 15, top, Pal.Orange);
            c.Line(x, top + 3, x + 15, top + 3, Pal.Plum);
            for (int i = 0; i < 16; i++) // grain: a wavy dark line inside each plank
                if ((i + p * 5) % 7 < 4) c.Px(x + i, top + 1 + ((i / 3 + p) % 2), Pal.Plum);
        }
        c.Px(x + 4 + 0, y + 6, Pal.Black).Px(x + 11, y + 13, Pal.Black); // two knots
        c.Line(x + 7, y, x + 7, y + 3, Pal.Black).Line(x + 12, y + 4, x + 12, y + 7, Pal.Black).Line(x + 3, y + 8, x + 3, y + 11, Pal.Black).Line(x + 9, y + 12, x + 9, y + 15, Pal.Black);
    }

    private static void Stone(ArtCanvas c, int x, int y)
    {
        c.Rect(x, y, x + 15, y + 15, Pal.Slate);
        // Mortar between irregular blocks, a light top-left edge on each block and a crack.
        c.Line(x, y + 6, x + 15, y + 6, Pal.Ebony).Line(x, y + 15, x + 15, y + 15, Pal.Ebony);
        c.Line(x + 6, y, x + 6, y + 5, Pal.Ebony).Line(x + 11, y + 7, x + 11, y + 14, Pal.Ebony).Line(x + 3, y + 7, x + 3, y + 14, Pal.Ebony);
        c.Line(x, y, x + 5, y, Pal.Silver).Line(x + 7, y, x + 15, y, Pal.Silver).Line(x, y + 7, x + 2, y + 7, Pal.Silver)
         .Line(x + 4, y + 7, x + 10, y + 7, Pal.Silver).Line(x + 12, y + 7, x + 15, y + 7, Pal.Silver)
         .Line(x + 7, y + 1, x + 7, y + 5, Pal.Silver).Line(x + 4, y + 8, x + 4, y + 14, Pal.Silver).Line(x + 12, y + 8, x + 12, y + 14, Pal.Silver);
        c.Px(x + 2, y + 2, Pal.Ebony).Px(x + 3, y + 3, Pal.Ebony).Px(x + 3, y + 4, Pal.Ebony);
        c.Px(x + 7, y + 10, Pal.Ebony).Px(x + 8, y + 11, Pal.Ebony).Px(x + 8, y + 12, Pal.Ebony);
        c.Px(x + 13, y + 2, Pal.Ebony).Px(x + 14, y + 11, Pal.Silver);
    }

    private static void Cloth(ArtCanvas c, int x, int y)
    {
        c.Rect(x, y, x + 15, y + 15, Pal.Blue);
        // Soft folds: a dark crease with a light ridge beside it, running diagonally.
        for (int k = -1; k < 3; k++)
        {
            int ox = x + k * 6;
            for (int i = 0; i < 16; i++)
            {
                int px = ox + i / 2;
                if (px >= x && px <= x + 15) c.Px(px, y + i, Pal.Navy);
                if (px + 1 >= x && px + 1 <= x + 15) c.Px(px + 1, y + i, Pal.Sky);
            }
        }
        for (int i = 0; i < 16; i += 2) c.Px(x + i, y + 1, Pal.Aqua); // a stitched hem
    }

    /// <summary>A shaded ball (orange ramp) with its bottom on row <paramref name="bottom"/>, centred on x = <paramref name="cx"/>.</summary>
    private static void Ball(ArtCanvas c, int cx, int bottom, int w, int h)
    {
        int x0 = cx - w / 2;
        c.Sphere(x0, bottom - h + 1, x0 + w - 1, bottom, new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow });
    }

    /// <summary>Draws <paramref name="draw"/> on a scratch canvas and copies it in as one flat tint (an onion-skin ghost).</summary>
    private static void Ghost(ArtCanvas c, int w, int h, Action<ArtCanvas> draw, Rgba tint)
    {
        var g = new ArtCanvas(w, h);
        draw(g);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (g.IsSet(x, y)) c.Px(x, y, tint);
    }

    private static readonly string[] HeroFront =
    {
        "..pppppp..",
        ".pppppppp.",
        "pppyyyyppp",
        "pyykyykyyp",
        "pyyyyyyyyp",
        ".yyyyoyyy.",
        "..yyyyyy..",
        ".bbbbbbbb.",
        "ybbbbbbbby",
        "ybbnbbnbby",
        "y.nnnnnn.y",
        "..ee..ee..",
        "..ee..ee..",
        ".kkk..kkk.",
    };

    private static readonly string[] HeroSide =
    {
        "..pppppp..",
        ".ppppppppp",
        "pppppyyyy.",
        "ppppyyyky.",
        "pppyyyyyyy",
        ".ppyyyyoy.",
        "...yyyyy..",
        "...bbbbb..",
        "..bbbbbb..",
        "..bbbybb..",
        "...nnnnn..",
        "...ee.ee..",
        "...ee.ee..",
        "...kkk.kkk",
    };

    private static readonly string[] HeroBack =
    {
        "..pppppp..",
        ".pppppppp.",
        "pppppppppp",
        "pppppppppp",
        "pppppppppp",
        ".pppppppp.",
        "..yyyyyy..",
        ".bbbbbbbb.",
        "ybbbbbbbby",
        "ybbbbbbbby",
        "y.nnnnnn.y",
        "..ee..ee..",
        "..ee..ee..",
        ".kkk..kkk.",
    };

    /// <summary>A 10×14 chibi hero (big head, two heads tall) at (x, y), without its outline.</summary>
    private static void Hero(ArtCanvas c, int x, int y, string[] rows) => c.Rows(x, y, rows);

    /// <summary>A taller hero (about five heads tall) in a 12×31 box at (x, y), without its outline.</summary>
    private static void Tall(ArtCanvas c, int x, int y)
    {
        c.Ellipse(x + 3, y, x + 8, y + 6, Pal.Yellow).Rect(x + 3, y, x + 8, y + 1, Pal.Plum).Px(x + 3, y + 2, Pal.Plum).Px(x + 8, y + 2, Pal.Plum);
        c.Px(x + 4, y + 3, Pal.Black).Px(x + 7, y + 3, Pal.Black);
        c.Rect(x + 5, y + 7, x + 6, y + 7, Pal.Yellow);
        c.Rect(x + 2, y + 8, x + 9, y + 17, Pal.Blue).Line(x + 2, y + 16, x + 9, y + 16, Pal.Navy);
        c.Rect(x, y + 9, x + 1, y + 17, Pal.Blue).Rect(x + 10, y + 9, x + 11, y + 17, Pal.Blue).Rect(x, y + 18, x + 1, y + 19, Pal.Yellow).Rect(x + 10, y + 18, x + 11, y + 19, Pal.Yellow);
        c.Rect(x + 2, y + 18, x + 9, y + 19, Pal.Navy);
        c.Rect(x + 2, y + 20, x + 4, y + 28, Pal.Ebony).Rect(x + 7, y + 20, x + 9, y + 28, Pal.Ebony);
        c.Rect(x + 1, y + 29, x + 4, y + 30, Pal.Black).Rect(x + 7, y + 29, x + 10, y + 30, Pal.Black);
    }

    /// <summary>Three mountain ranges: with depth (far = pale, low contrast; near = dark, saturated) or all alike.</summary>
    private static void Ranges(ArtCanvas c, bool depth)
    {
        for (int y = 0; y < 32; y++) c.Line(0, y, 63, y, depth ? Pal.Mix(Pal.Sky, Pal.Aqua, y / 31.0) : Pal.Sky);
        var far = depth ? (Pal.Mix(Pal.Silver, Pal.Aqua, 0.5), Pal.Mix(Pal.Silver, Pal.White, 0.4)) : (Pal.Green, Pal.Lime);
        var mid = depth ? (Pal.Slate, Pal.Silver) : (Pal.Green, Pal.Lime);
        var near = depth ? (Pal.Teal, Pal.Green) : (Pal.Green, Pal.Lime);
        Ridge(c, x => 12 + 4 * Math.Sin(x / 7.0) + 2 * Math.Sin(x / 2.3), far.Item1, far.Item2, outline: !depth);
        Ridge(c, x => 18 + 3 * Math.Sin(x / 5.0 + 2), mid.Item1, mid.Item2, outline: !depth);
        Ridge(c, x => 25 + 2 * Math.Sin(x / 4.0 + 1), near.Item1, near.Item2, outline: !depth);
    }

    private static void Ridge(ArtCanvas c, Func<int, double> top, Rgba fill, Rgba edge, bool outline)
    {
        for (int x = 0; x < c.Width; x++)
        {
            int t = (int)Math.Round(top(x));
            c.Line(x, t, x, c.Height - 1, fill).Px(x, t, outline ? Pal.Black : edge);
            if (outline) c.Px(x, t + 1, edge);
        }
    }

    /// <summary>A hills layer for parallax: filled from the ridge down to the bottom edge; transparent sky above.</summary>
    private static void Hills(ArtCanvas c, Func<int, double> top)
    {
        for (int x = 0; x < c.Width; x++)
        {
            int t = (int)Math.Round(top(x));
            c.Line(x, t, x, c.Height - 1, Pal.Teal).Px(x, t, Pal.Green);
            if (x % 8 is 2 or 5) c.Px(x, Math.Min(c.Height - 1, t + (x % 8 == 2 ? 3 : 5)), Pal.Green); // a period that divides the width, so the strip wraps
        }
    }

    /// <summary>One school window (6 wide, 8 tall): white frame, sky-blue glass with a reflection, a cross bar.</summary>
    private static void SchoolWindow(ArtCanvas c, int x, int y, int w = 6, int h = 8)
    {
        c.Rect(x, y, x + w - 1, y + h - 1, Pal.White);
        c.Rect(x + 1, y + 1, x + w - 2, y + h - 2, Pal.Sky);
        c.Line(x + 1, y + h / 2, x + w - 2, y + h / 2, Pal.White);
        c.Px(x + 1, y + 1, Pal.Aqua);
    }

    private static void WindowRow(ArtCanvas c, (int X, int Y, int W, int H)[] windows)
    {
        c.Rect(0, 0, c.Width - 1, c.Height - 1, Pal.Red);
        foreach (var (x, y, w, h) in windows) SchoolWindow(c, x, y, w, h);
    }

    /// <summary>The school in side view: one flat facade, windows copied in rows, a door with steps, a clock on the gable.</summary>
    private static void SchoolSide(ArtCanvas c)
    {
        c.Rect(6, 14, 57, 45, Pal.Red);                       // brick walls
        for (int y = 17; y < 45; y += 4) c.Line(6, y, 57, y, Pal.Plum);
        c.Rect(4, 12, 59, 14, Pal.Slate).Line(4, 12, 59, 12, Pal.Silver); // the roof's edge
        for (int y = 3; y <= 11; y++) c.Line(32 - (y - 3) * 2 - 6, y, 31 + (y - 3) * 2 + 6, y, Pal.Red); // gable
        c.Line(20, 11, 31, 3, Pal.Silver).Line(32, 3, 43, 11, Pal.Silver);
        c.Ellipse(28, 4, 35, 11, Pal.White).Line(31, 6, 31, 8, Pal.Black).Line(32, 8, 33, 8, Pal.Black);
        c.Rect(23, 26, 40, 29, Pal.Yellow);                   // the sign over the door
        for (int x = 25; x <= 38; x += 2) c.Px(x, 27, Pal.Black).Px(x, 28, x % 4 == 1 ? Pal.Black : Pal.Yellow);
        c.Rect(26, 32, 37, 44, Pal.Navy).Line(31, 32, 31, 44, Pal.Black).Line(32, 32, 32, 44, Pal.Ebony);
        c.Px(30, 38, Pal.Yellow).Px(33, 38, Pal.Yellow);
        SchoolWindow(c, 9, 17);                                // one window, copied to every place
        foreach (var (wx, wy) in new[] { (17, 17), (40, 17), (48, 17), (9, 31), (17, 31), (40, 31), (48, 31) }) c.Copy(9, 17, 6, 8, wx, wy);
        c.Outline(Pal.Black);
        c.Rect(23, 45, 40, 45, Pal.Silver).Rect(21, 46, 42, 46, Pal.Silver);
        c.Line(0, 47, 63, 47, Pal.Green).Line(0, 46, 20, 46, Pal.Green).Line(43, 46, 63, 46, Pal.Green);
        c.Ellipse(0, 38, 7, 46, Pal.Green).Ellipse(1, 39, 4, 42, Pal.Lime).Ellipse(57, 39, 63, 46, Pal.Green).Ellipse(58, 40, 60, 42, Pal.Lime);
    }

    /// <summary>The school in 3/4 view (top-down RPG): the roof seen from above, then a shorter front wall below it.</summary>
    private static void School34(ArtCanvas c)
    {
        c.Rect(0, 0, 63, 47, Pal.Green);
        for (int i = 0; i < 40; i++) c.Px(Hash(i, 1) % 64, Hash(1, i) % 48, Pal.Lime);
        c.Rect(28, 40, 35, 47, Pal.Silver);                    // the path to the door
        c.Rect(10, 39, 59, 41, Pal.Teal);                       // the shadow, cast down-right
        c.Rect(6, 4, 57, 22, Pal.Navy);                         // roof
        for (int y = 6; y <= 20; y += 3) c.Line(6, y, 57, y, Pal.Blue);
        c.Line(6, 4, 57, 4, Pal.Sky).Line(6, 21, 57, 22, Pal.Ebony).Line(6, 22, 57, 22, Pal.Ebony);
        c.Rect(26, 1, 37, 6, Pal.Red).Ellipse(29, 1, 34, 6, Pal.White).Px(31, 3, Pal.Black).Px(32, 4, Pal.Black); // the clock tower top
        c.Rect(6, 23, 57, 38, Pal.Red);                         // front wall: shorter than in side view
        c.Line(6, 23, 57, 23, Pal.Plum);
        SchoolWindow(c, 9, 26, 6, 7);
        foreach (var wx in new[] { 17, 40, 48 }) c.Copy(9, 26, 6, 7, wx, 26);
        c.Rect(27, 29, 36, 38, Pal.Navy).Line(31, 29, 31, 38, Pal.Black).Px(30, 34, Pal.Yellow).Px(33, 34, Pal.Yellow);
        c.Rect(25, 25, 38, 27, Pal.Yellow);
        c.Rect(5, 3, 58, 38, Pal.Black, filled: false);
    }

    /// <summary>The school seen straight from above: only the roof shows; the door is a small awning on one edge.</summary>
    private static void SchoolTop(ArtCanvas c)
    {
        c.Rect(0, 0, 63, 47, Pal.Green);
        for (int i = 0; i < 40; i++) c.Px(Hash(i, 2) % 64, Hash(2, i) % 48, Pal.Lime);
        c.Rect(28, 38, 35, 47, Pal.Silver);
        c.Rect(9, 9, 59, 39, Pal.Teal);                          // the shadow shows the height
        c.Rect(6, 6, 56, 36, Pal.Navy);
        c.Line(6, 21, 56, 21, Pal.Sky);                          // the ridge
        for (int y = 8; y <= 34; y += 3) if (y != 21) c.Line(7, y, 55, y, Pal.Blue);
        c.Rect(12, 11, 17, 16, Pal.Aqua).Rect(45, 26, 50, 31, Pal.Aqua);       // skylights
        c.Rect(40, 10, 45, 14, Pal.Silver).Rect(41, 11, 44, 13, Pal.Slate);    // an air-conditioning box
        c.Rect(26, 36, 37, 38, Pal.Red);                         // the awning over the door
        c.Rect(5, 5, 57, 37, Pal.Black, filled: false);
    }

    private static void SchoolProps(ArtCanvas c)
    {
        // A desk with a chair, a chalkboard, a locker and a bin, all lit from the top-left.
        c.Rect(1, 10, 14, 11, Pal.Orange).Line(1, 12, 14, 12, Pal.Red).Rect(2, 13, 2, 20, Pal.Red).Rect(13, 13, 13, 20, Pal.Red);
        c.Rect(16, 8, 17, 15, Pal.Plum).Rect(16, 15, 22, 16, Pal.Red).Rect(16, 17, 16, 20, Pal.Plum).Rect(22, 17, 22, 20, Pal.Plum);
        c.Rect(25, 1, 44, 13, Pal.Plum).Rect(26, 2, 43, 12, Pal.Teal);
        c.Line(28, 4, 33, 4, Pal.White).Line(28, 6, 36, 6, Pal.White).Line(28, 9, 31, 9, Pal.White).Px(40, 11, Pal.White);
        c.Rect(47, 2, 54, 20, Pal.Slate).Line(47, 2, 47, 20, Pal.Silver).Line(54, 2, 54, 20, Pal.Ebony);
        for (int y = 4; y <= 8; y += 2) c.Line(49, y, 52, y, Pal.Ebony);
        c.Px(52, 12, Pal.Yellow);
        c.Rect(56, 14, 59, 20, Pal.Green).Line(56, 14, 59, 14, Pal.Lime);
        c.Outline(Pal.Black);
    }

    private static void IconSet(ArtCanvas c)
    {
        Sword(c, 0, 0, good: true);
        Potion(c, 18, 0);
        Key(c, 36, 0);
        Coin(c, 54, 0);
        Shield(c, 72, 0);
        c.Outline(Pal.Black);
    }

    /// <summary>A 16×16 sword. Good: a bold two-pixel blade, few colours (outlined by the caller). Bad: thin, many colours, specks, no outline.</summary>
    private static void Sword(ArtCanvas c, int x, int y, bool good)
    {
        if (good)
        {
            for (int i = 0; i < 8; i++) c.Px(x + 6 + i, y + 9 - i, Pal.White).Px(x + 7 + i, y + 9 - i, Pal.Silver);
            c.Px(x + 14, y + 1, Pal.White);
            c.Line(x + 3, y + 8, x + 7, y + 12, Pal.Yellow).Px(x + 7, y + 12, Pal.Orange).Px(x + 6, y + 11, Pal.Orange);
            c.Line(x + 4, y + 11, x + 2, y + 13, Pal.Plum).Px(x + 1, y + 14, Pal.Yellow);
        }
        else
        {
            c.Line(x + 6, y + 9, x + 14, y + 1, Pal.White).Px(x + 10, y + 5, Pal.Aqua).Px(x + 12, y + 3, Pal.Sky);
            c.Line(x + 4, y + 8, x + 7, y + 11, Pal.Silver).Px(x + 5, y + 9, Pal.Lime).Px(x + 6, y + 10, Pal.Green);
            c.Px(x + 4, y + 11, Pal.Red).Px(x + 3, y + 12, Pal.Plum).Px(x + 2, y + 13, Pal.Orange).Px(x + 1, y + 14, Pal.Yellow).Px(x + 0, y + 15, Pal.Teal);
            c.Px(x + 9, y + 9, Pal.Slate).Px(x + 13, y + 6, Pal.Navy).Px(x + 3, y + 3, Pal.Blue);
        }
    }

    private static void Key(ArtCanvas c, int x, int y)
    {
        c.Ellipse(x + 1, y + 4, x + 7, y + 10, Pal.Yellow).Ellipse(x + 3, y + 6, x + 5, y + 8, Rgba.Transparent);
        c.Rect(x + 7, y + 6, x + 14, y + 8, Pal.Yellow).Line(x + 7, y + 8, x + 14, y + 8, Pal.Orange);
        c.Rect(x + 11, y + 9, x + 12, y + 11, Pal.Orange).Rect(x + 14, y + 9, x + 14, y + 10, Pal.Orange);
        c.Px(x + 2, y + 5, Pal.White).Px(x + 8, y + 6, Pal.White);
    }

    private static void Coin(ArtCanvas c, int x, int y)
    {
        c.Ellipse(x + 2, y + 1, x + 13, y + 14, Pal.Yellow);
        c.Shade((px, py, col) => px >= x && px < x + 16 && col == Pal.Yellow && (px - x) + (py - y) > 19 ? Pal.Orange : null);
        c.Line(x + 7, y + 4, x + 7, y + 11, Pal.Orange).Line(x + 8, y + 4, x + 8, y + 11, Pal.White);
        c.Px(x + 5, y + 3, Pal.White).Px(x + 4, y + 4, Pal.White);
    }

    private static void Shield(ArtCanvas c, int x, int y)
    {
        c.Rows(x + 2, y + 1,
            "ssssssssssss",
            "sbbbbyybbbbs",
            "sbcbbyybbbbs",
            "sbbbbyybbbbs",
            "yyyyyyyyyyyy",
            "sbbbbyybbbns",
            "sbbbbyybbbns",
            ".sbbbyybbns.",
            ".sbbbyybnns.",
            "..sbbyybns..",
            "...sbyyns...",
            "....syys....",
            ".....ss.....");
    }

    /// <summary>A flame (one frame of a 4-frame loop): red outside, orange, a yellow core, flickering at the tip.</summary>
    private static void Flame(ArtCanvas c, int f)
    {
        int top = 3 + (f % 2) * 2;
        for (int y = top; y <= 21; y++)
        {
            double t = (y - top) / (double)(21 - top);                    // 0 at the tip, 1 at the base
            double half = 1 + 5.5 * Math.Sin(Math.Min(1, t * 1.15) * Math.PI / 2) - (t > 0.85 ? (t - 0.85) * 12 : 0);
            double mid = 8 + Math.Sin(y * 0.7 + f * Math.PI / 2) * (1 - t) * 2;
            for (int x = 0; x < 16; x++)
            {
                double d = Math.Abs(x + 0.5 - mid) / Math.Max(0.5, half);
                if (d > 1) continue;
                c.Px(x, y, d > 0.7 ? Pal.Red : d > 0.4 || t < 0.35 ? Pal.Orange : t > 0.7 && d < 0.2 ? Pal.White : Pal.Yellow);
            }
        }
        if (f == 1) c.Px(10, 1, Pal.Orange);
        if (f == 2) c.Px(5, 0, Pal.Orange).Px(6, 2, Pal.Red);
        if (f == 3) c.Px(9, 0, Pal.Red);
        c.Rect(3, 21, 12, 22, Pal.Plum).Line(3, 23, 12, 23, Pal.Black);
    }

    /// <summary>One frame of a smoke puff: rising, growing, getting lighter and thinner (checker thinning) until it's gone.</summary>
    private static void Smoke(ArtCanvas c, int f)
    {
        var shades = new[] { Pal.Ebony, Pal.Slate, Pal.Slate, Pal.Silver, Pal.Silver, Pal.White };
        double r = 2.5 + f * 0.9;
        int cy = 21 - f * 3;
        foreach (var (dx, dy, k) in new[] { (-2, 0, 1.0), (2, -1, 0.9), (0, -3, 0.8) })
        {
            double rr = r * k, px = 10 + dx * (1 + f * 0.3), py = cy + dy * (1 + f * 0.2);
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    double ddx = x + 0.5 - px, ddy = y + 0.5 - py;
                    if (ddx * ddx + ddy * ddy > rr * rr) continue;
                    if (f >= 3 && (x + y) % 2 == 0) continue;          // thinning out
                    if (f >= 5 && (x + 2 * y) % 3 != 0) continue;      // almost gone
                    bool lit = ddx + ddy < -rr * 0.3;
                    c.Px(x, y, lit ? Pal.Mix(shades[f], Pal.White, 0.4) : shades[f]);
                }
        }
    }

    /// <summary>One frame of an explosion: a flash, a fireball, a ring, smoke, then breaking up.</summary>
    private static void Explosion(ArtCanvas c, int f)
    {
        void Disc(double cx, double cy, double r, Rgba col, Func<int, int, bool>? keep = null)
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                    if (dx * dx + dy * dy <= r * r && (keep == null || keep(x, y))) c.Px(x, y, col);
                }
        }
        switch (f)
        {
            case 0: Disc(16, 16, 3, Pal.Yellow); Disc(16, 16, 1.6, Pal.White); break;
            case 1: Disc(16, 16, 8, Pal.Orange); Disc(16, 16, 6, Pal.Yellow); Disc(15, 15, 3.5, Pal.White); break;
            case 2:
                Disc(16, 16, 12, Pal.Red); Disc(16, 15, 10, Pal.Orange); Disc(15, 14, 6.5, Pal.Yellow); Disc(14, 13, 2.5, Pal.White);
                foreach (var (x, y) in new[] { (3, 6), (28, 4), (29, 26), (4, 27) }) c.Px(x, y, Pal.Yellow);
                break;
            case 3:
                Disc(16, 16, 14, Pal.Red); Disc(16, 15, 12, Pal.Orange); Disc(16, 15, 8, Pal.Slate); Disc(15, 14, 5, Pal.Silver);
                foreach (var (x, y) in new[] { (1, 3), (30, 2), (31, 29), (2, 30) }) c.Px(x, y, Pal.Orange);
                break;
            case 4:
                foreach (var (x, y, r) in new[] { (10.0, 12.0, 6.0), (21.0, 11.0, 6.0), (16.0, 20.0, 7.0), (8.0, 21.0, 4.0), (24.0, 21.0, 4.0) })
                { Disc(x, y, r, Pal.Slate); Disc(x - 1, y - 1, r * 0.6, Pal.Silver); }
                Disc(16, 17, 3, Pal.Orange);
                break;
            case 5:
                foreach (var (x, y, r) in new[] { (9.0, 10.0, 6.0), (23.0, 9.0, 6.0), (16.0, 20.0, 7.0) })
                    Disc(x, y, r, Pal.Silver, (px, py) => (px + py) % 2 == 0);
                break;
            default:
                foreach (var (x, y, r) in new[] { (8.0, 8.0, 4.0), (24.0, 7.0, 4.0), (16.0, 21.0, 4.0) })
                    Disc(x, y, r, Pal.Silver, (px, py) => (px + 2 * py) % 5 == 0);
                break;
        }
    }

    /// <summary>A twinkle: a plus of <paramref name="arm"/> pixels each way (with short diagonals at its brightest).</summary>
    private static void Sparkle(ArtCanvas c, int cx, int cy, int arm, bool diagonal)
    {
        for (int i = 1; i <= arm; i++)
        {
            var col = i <= arm / 2 ? Pal.Yellow : Pal.Orange;
            c.Px(cx + i, cy, col).Px(cx - i, cy, col).Px(cx, cy + i, col).Px(cx, cy - i, col);
        }
        if (diagonal) for (int i = 1; i <= 2; i++) c.Px(cx + i, cy + i, Pal.Aqua).Px(cx - i, cy - i, Pal.Aqua).Px(cx + i, cy - i, Pal.Aqua).Px(cx - i, cy + i, Pal.Aqua);
        c.Px(cx, cy, Pal.White);
    }

    /// <summary>The 4-frame walk with each frame placed at an offset: steady offsets stand still, mixed ones jitter.</summary>
    private static void AlignedWalk(ArtCanvas c, (int X, int Y)[] offsets)
    {
        int[] front = { 9, 6, 2, 5 }, back = { 2, 5, 9, 6 }, arm = { 3, 6, 9, 6 };
        for (int f = 0; f < 4; f++)
        {
            if (f > 0) c.NewFrame(durationMs: 150); else c.Duration(150);
            WalkerPose(c, offsets[f].X, offsets[f].Y, front[f], back[f], arm[f], f % 2 == 1 ? -1 : 0);
            c.Outline(Pal.Black);
        }
    }
}
