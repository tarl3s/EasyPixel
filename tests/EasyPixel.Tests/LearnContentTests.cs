using EasyPixel.Core.Formats;
using EasyPixel.Core.Learn;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tests;

/// <summary>The shipped lessons, illustrations and exercises fit together.</summary>
public class LearnContentTests
{
    public static IEnumerable<object[]> LessonFiles =>
        LessonLibrary.Languages.SelectMany(lang => LessonLibrary.Ids.Where(id => LessonLibrary.HasTranslation(id, lang)).Select(id => new object[] { lang, id }));

    [Fact]
    public void Library_HasTheWrittenLessons_InOrder()
    {
        var ids = LessonLibrary.Ids;
        foreach (var n in new[] { "01-", "02-", "03-", "05-", "06-", "13-" })
            Assert.Contains(ids, id => id.StartsWith(n, StringComparison.Ordinal));
        Assert.Equal(ids.OrderBy(i => i, StringComparer.Ordinal), ids);
        Assert.Contains("en", LessonLibrary.Languages);
    }

    /// <summary>Every lesson file in every language parses cleanly and names only illustrations and exercises that exist.</summary>
    [Theory]
    [MemberData(nameof(LessonFiles))]
    public void LessonFile_IsCleanAndReferencesExist(string lang, string id)
    {
        var lesson = LessonParser.Parse(id, LessonLibrary.ReadRaw(lang, id));
        Assert.True(lesson.Warnings.Count == 0, $"{lang}/{id}: " + string.Join(" | ", lesson.Warnings));
        Assert.NotEqual(id, lesson.Title);
        Assert.False(string.IsNullOrWhiteSpace(lesson.Summary), $"{lang}/{id} has no summary");
        Assert.True(LessonLibrary.HasTranslation(id, "en"), $"{lang}/{id} has no English original");
        var missing = lesson.ExampleIds.Where(e => !LessonArt.Exists(e)).ToList();
        Assert.True(missing.Count == 0, $"{lang}/{id} shows unknown illustrations: " + string.Join(", ", missing));
        foreach (var t in lesson.Blocks.OfType<TryItBlock>())
            Assert.True(Exercises.Exists(t.ExerciseId), $"{lang}/{id} names unknown exercise '{t.ExerciseId}'");
        Assert.NotNull(lesson.TryIt);
        Assert.NotEmpty(lesson.TryIt!.Goal);
        Assert.NotEmpty(lesson.ExampleIds);
        if (lang != "en") // a translation has the same structure as the English page
        {
            var en = LessonLibrary.Load(id, "en").Lesson;
            Assert.Equal(en.Group, lesson.Group);
            Assert.Equal(en.TryIt!.ExerciseId, lesson.TryIt.ExerciseId);
            Assert.Equal(en.TryIt.SelfChecks.Count, lesson.TryIt.SelfChecks.Count);
        }
    }

    [Fact]
    public void Load_FallsBackToEnglish_WithAFlag()
    {
        var id = LessonLibrary.Ids[0];
        var en = LessonLibrary.Load(id, "en");
        Assert.False(en.IsFallback);
        var de = LessonLibrary.Load(id, "de");
        Assert.Equal(LessonLibrary.HasTranslation(id, "de"), !de.IsFallback);
        if (de.IsFallback) Assert.Same(en.Lesson, de.Lesson);
        Assert.True(LessonLibrary.Load(id, "xx-nothing").IsFallback);
        Assert.False(LessonLibrary.Load(id, "").IsFallback); // no language = English
        Assert.Throws<KeyNotFoundException>(() => LessonLibrary.Load("99-nope", "en"));
        Assert.Equal(LessonLibrary.Ids.Count, LessonLibrary.All("ja").Count);
    }

    [Fact]
    public void WrittenLessons_AreSubstantial()
    {
        foreach (var id in LessonLibrary.Ids.Where(i => i[..3] is "01-" or "02-" or "03-" or "05-" or "06-" or "13-"))
        {
            var l = LessonLibrary.Load(id, "en").Lesson;
            int words = l.Blocks.Sum(b => b switch
            {
                ParagraphBlock p => Words(p.Text),
                ListBlock li => li.Items.Sum(Words),
                TipBlock t => Words(t.Text),
                MistakeBlock m => Words(m.Text),
                _ => 0,
            });
            Assert.True(words >= 600, $"{id}: only {words} words");
            Assert.True(l.Blocks.OfType<MistakeBlock>().Any(), $"{id} names no common mistake");
            Assert.True(l.Blocks.OfType<TipBlock>().Any(), $"{id} has no tip");
            Assert.True(l.ExampleIds.Count() >= 3, $"{id} has fewer than 3 illustrations");
            Assert.NotEmpty(l.TryIt!.SelfChecks);
        }

        static int Words(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // ---- illustrations ----

    [Fact]
    public void EveryIllustration_Builds()
    {
        var dump = Environment.GetEnvironmentVariable("EASYPIXEL_ART_DUMP"); // set it to look at them as PNGs
        foreach (var id in LessonArt.Ids)
        {
            var ex = LessonArt.Build(id);
            Assert.True(ex.Zoom >= 1, id);
            Assert.Equal(id, ex.Id);
            for (int f = 0; f < ex.Sprite.Frames.Count; f++)
            {
                var img = ex.Sprite.Cels[(ex.Sprite.Layers[0], f)].Image;
                Assert.True(img.Pixels.Any(p => (p >> 24) != 0), $"{id} frame {f} is empty");
                if (dump != null) File.WriteAllBytes(Path.Combine(dump, $"{id}-{f}.png"), Png.Encode(Scale(img, ex.Zoom)));
            }
        }
        Assert.Throws<KeyNotFoundException>(() => LessonArt.Build("nope"));
        Assert.False(LessonArt.Exists("nope"));
    }

    private static Image Scale(Image img, int z)
    {
        var o = new Image(img.Width * z, img.Height * z);
        for (int y = 0; y < o.Height; y++)
            for (int x = 0; x < o.Width; x++) o.Pixels[y * o.Width + x] = img.Get(x / z, y / z);
        return o;
    }

    [Fact]
    public void AnimatedIllustrations_Move()
    {
        foreach (var (id, frames) in new[] { ("walk-4frame", 4), ("idle-bob", 2), ("parallax-layers", 16), ("walk-6frame", 6), ("bounce-ball", 8), ("fx-fire", 4),
            ("fx-smoke", 6), ("fx-explosion", 7), ("fx-sparkle", 4), ("frames-aligned-good", 4), ("frames-aligned-bad", 4) })
        {
            var s = LessonArt.Build(id).Sprite;
            Assert.Equal(frames, s.Frames.Count);
            Assert.True(LessonArt.Build(id).Animated);
            Passes(Rules.HasFrames(frames), Art(id));
        }
        Assert.False(LessonArt.Build("potion-16").Animated);
    }

    private static CheckInput Art(string id)
    {
        var s = LessonArt.Build(id).Sprite;
        return CheckInput.Of(Enumerable.Range(0, s.Frames.Count).Select(f => s.Cels[(s.Layers[0], f)].Image).ToArray());
    }

    private static void Passes(CheckRule r, CheckInput i) { var l = r.Check(i); Assert.True(l.Ok, l.English); }
    private static void Fails(CheckRule r, CheckInput i) { var l = r.Check(i); Assert.False(l.Ok, "expected a failure: " + l.English); }

    /// <summary>The good/bad illustrations show what the rules judge.</summary>
    [Fact]
    public void Illustrations_AgreeWithTheRules()
    {
        Fails(Rules.HasOutline(), Art("outline-none"));
        Passes(Rules.HasOutline(), Art("outline-black"));
        Passes(Rules.HasOutline(), Art("outline-colored"));
        Passes(Rules.HasOutline(), Art("outline-selout"));
        Passes(Rules.LightFrom(), Art("light-source"));
        Fails(Rules.LightFrom(), Art("pillow-shading"));
        Passes(Rules.PixelPerfect(), Art("doubles-good"));
        Fails(Rules.PixelPerfect(), Art("doubles-bad"));
        Passes(Rules.PixelPerfect(), Art("jaggies-good"));
        Passes(Rules.PixelPerfect(), Art("curve-good"));
        Passes(Rules.PixelPerfect(), Art("slopes"));
        Passes(Rules.Seamless(), Art("tile-single"));
        Passes(Rules.FullyOpaque(), Art("tile-single"));
        Passes(Rules.Seamless(), Art("tile-seam-good"));
        Fails(Rules.Seamless(), Art("tile-seam-bad"));
        var pts = new[] { (2, 2), (8, 2), (14, 2), (20, 2) };
        Passes(Rules.RampOrdered(pts), Art("ramp-hue-shift"));
        Passes(Rules.HueShift((2, 2), (20, 2)), Art("ramp-hue-shift"));
        Passes(Rules.RampOrdered(pts), Art("ramp-flat"));
        Fails(Rules.HueShift((2, 2), (20, 2)), Art("ramp-flat"));
        Passes(Rules.UsesDither(), Art("dither-checker"));
        Passes(Rules.MaxColors(8), Art("potion-16"));
        Passes(Rules.Contrast(min: 120), Art("value-good"));
        Fails(Rules.Contrast(min: 120), Art("value-bad"));
        var aa = Rules.AntiAliased(Pal.Black, Pal.White, 6);
        Passes(aa, Art("aa-good"));
        Fails(aa, Art("aa-none"));
        Fails(aa, Art("aa-overdone"));
        Passes(Rules.UsesDither(), Art("dither-smooth"));
        Fails(Rules.UsesDither(), Art("dither-banding"));
        Passes(Rules.GetsLighter(Toward.Right), Art("dither-smooth"));
        Passes(Rules.GetsLighter(Toward.Right), Art("dither-banding"));
        Passes(Rules.Texture(0.1, 0.45), Art("texture-clusters"));
        Fails(Rules.Texture(0.1, 0.45), Art("texture-noise"));
        foreach (var walk in new[] { "walk-4frame", "walk-6frame" })
        {
            Passes(Rules.OnFloor(20), Art(walk));
            Passes(Rules.InPlace(3), Art(walk));
            Passes(Rules.Bobs(1), Art(walk));
        }
        Passes(Rules.OnFloor(20), Art("frames-aligned-good"));
        Fails(Rules.OnFloor(20), Art("frames-aligned-bad"));
        Passes(Rules.Seamless(horizontal: true, vertical: false), Art("hills-strip"));
        Passes(Rules.FilledBottom(), Art("hills-strip"));
        Fails(Rules.Seamless(horizontal: true, vertical: false), Art("hills-strip-bad"));
        Passes(Rules.Repeats(3), Art("windows-good"));
        Fails(Rules.Repeats(3), Art("windows-bad"));
        Passes(Rules.Repeats(3), Art("school-side"));
        Passes(Rules.HasOutline(), Art("icon-detail-good"));
        Passes(Rules.MaxColors(8), Art("icon-detail-good"));
        Fails(Rules.HasOutline(), Art("icon-detail-bad"));
        Fails(Rules.MaxColors(8), Art("icon-detail-bad"));
        Passes(Rules.GrowsThenFades(), Art("fx-explosion"));
    }

    // ---- exercises ----

    [Fact]
    public void EveryExercise_BuildsItsDocument_AndStartsUnpassed()
    {
        foreach (var e in Exercises.All)
        {
            var s = e.CreateDocument();
            Assert.Equal((e.Width, e.Height), (s.Width, s.Height));
            Assert.Equal(2, s.Layers.Count);
            Assert.True(Exercise.IsGuide(s, s.Layers[0]));
            Assert.False(Exercise.IsGuide(s, s.Layers[1]));
            Assert.True(s.Layers[0].Locked);
            Assert.Equal(Exercise.WorkLayerName, s.Layers[1].Name);
            Assert.False(s.Layers[1].Locked);
            Assert.Equal(e.Palette, s.Palette.Colors);
            Assert.Equal(e.Frames, s.Frames.Count);
            var r = e.Check(s);
            Assert.Equal(e.Rules.Count, r.Lines.Count);
            Assert.False(r.Passed, e.Id + " passes before anything is drawn");
            Assert.True(s.Layers[0].Visible && s.Layers[1].Visible); // checking leaves visibility as it was
        }
        Assert.Throws<KeyNotFoundException>(() => Exercises.Get("nope"));
    }

    /// <summary>A good answer to each exercise passes its check.</summary>
    public static IEnumerable<object[]> ExerciseIds => Exercises.All.Select(e => new object[] { e.Id });

    [Theory]
    [MemberData(nameof(ExerciseIds))]
    public void EveryExercise_HasAPassingAnswer(string id)
    {
        var e = Exercises.Get(id);
        var s = e.CreateDocument();
        var work = s.Layers[1];
        var c = new ArtCanvas(s.Width, s.Height);
        s.GetOrCreateCel(work, 0).Image.Pixels.CopyTo(c.Image.Pixels, 0);
        switch (id)
        {
            case "first-sprite": c.Ellipse(3, 3, 12, 12, Pal.Red).Ellipse(5, 5, 7, 7, Pal.Orange).Outline(Pal.Black); break;
            case "clean-lines": c.Line(2, 2, 9, 9, Pal.Black).Line(12, 2, 27, 9, Pal.Black).Line(2, 13, 19, 18, Pal.Black); break;
            case "silhouette":
                c.Ellipse(12, 3, 19, 10, Pal.Black).Rect(11, 11, 20, 21, Pal.Black).Rect(11, 22, 13, 28, Pal.Black).Rect(18, 22, 20, 28, Pal.Black)
                    .Line(21, 12, 27, 6, Pal.Black).Rect(6, 12, 10, 18, Pal.Black);
                break;
            case "shade-ball": c.Sphere(4, 4, 27, 27, new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow }); break;
            case "outline-apple": c.Outline(Pal.Black); break;
            case "seamless-tile":
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) c.Px(x, y, ((x + 2 * y) % 4) switch { 0 => Pal.Lime, 1 => Pal.Green, _ => Pal.Teal });
                break;
            case "palette-ramp":
                var ramp = new[] { Pal.Black, Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow };
                for (int i = 0; i < 5; i++) c.Rect(2 + i * 8, 2, 7 + i * 8, 7, ramp[i]);
                break;
            case "aa-curve": LessonArt.AntiAlias(c, Pal.Black, 28, 28, 23, Pal.Slate, Pal.Silver); break;
            case "dither-sky":
                var sky = new[] { Pal.Navy, Pal.Blue, Pal.Sky };
                for (int y = 0; y < 24; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        double t = y / 23.0 * 2;
                        int i = Math.Min(1, (int)t);
                        c.Px(x, y, Dither.Pattern(DitherKind.Bayer4, x, y, t - i) ? sky[i + 1] : sky[i]);
                    }
                break;
            case "material-block": // bricks: mortar rows, staggered joints, a few light chips
                c.Rect(0, 0, 15, 15, Pal.Red);
                for (int y = 3; y < 16; y += 4)
                {
                    c.Line(0, y, 15, y, Pal.Plum);
                    int off = (y / 4) % 2 == 0 ? 5 : 1;
                    c.Line(off, y - 3, off, y - 1, Pal.Plum).Line(off + 8, y - 3, off + 8, y - 1, Pal.Plum);
                }
                c.Px(2, 0, Pal.Orange).Px(10, 4, Pal.Orange).Px(6, 8, Pal.Orange).Px(13, 12, Pal.Orange);
                break;
            case "bouncing-ball":
                int[] bottoms = { 29, 22, 16, 22, 29 };
                for (int f = 0; f < bottoms.Length; f++)
                {
                    if (f > 0) c.NewFrame();
                    c.Ellipse(13, bottoms[f] - 5, 18, bottoms[f], Pal.Red).Px(14, bottoms[f] - 4, Pal.Yellow);
                }
                break;
            case "walk-cycle":
                int[] near = { 8, 11, 14, 11 }, far = { 14, 11, 8, 12 };
                for (int f = 0; f < 4; f++)
                {
                    if (f > 0) c.NewFrame();
                    int top = f % 2 == 1 ? 9 : 10;
                    c.Ellipse(9, top, 14, top + 5, Pal.Orange).Rect(9, top + 6, 14, 20, Pal.Blue);
                    c.Line(10, 21, near[f], 29, Pal.Navy).Line(13, 21, far[f], 29, Pal.Ebony);
                }
                break;
            case "hero-sprite":
                c.Ellipse(7, 6, 16, 15, Pal.Yellow).Rect(7, 6, 16, 8, Pal.Plum).Rect(8, 16, 15, 23, Pal.Blue)
                    .Rect(9, 24, 10, 28, Pal.Navy).Rect(13, 24, 14, 28, Pal.Navy).Outline(Pal.Black);
                break;
            case "parallax-hills":
                for (int x = 0; x < 64; x++)
                {
                    int t = (int)Math.Round(16 + 4 * Math.Sin(2 * Math.PI * x / 64));
                    c.Line(x, t, x, 31, Pal.Teal).Px(x, t, Pal.Green);
                }
                break;
            case "school-front":
                c.Rect(4, 10, 43, 45, Pal.Red).Rect(2, 7, 45, 9, Pal.Slate).Rect(20, 34, 27, 45, Pal.Navy);
                for (int i = 0; i < 4; i++) c.Rect(7 + i * 9, 14, 11 + i * 9, 19, Pal.Sky);
                break;
            case "item-icon":
                c.Ellipse(2, 2, 13, 13, Pal.Yellow).Shade((x, y, _) => x + y > 17 ? Pal.Orange : null).Px(5, 5, Pal.White).Outline(Pal.Black);
                break;
            case "explosion":
                int[] radius = { 2, 6, 9, 6, 2 };
                for (int f = 0; f < radius.Length; f++)
                {
                    if (f > 0) c.NewFrame();
                    int rad = radius[f];
                    c.Ellipse(12 - rad, 12 - rad, 12 + rad, 12 + rad, f < 3 ? Pal.Orange : Pal.Slate);
                    if (f < 3) c.Ellipse(12 - rad / 2, 12 - rad / 2, 12 + rad / 2, 12 + rad / 2, Pal.Yellow);
                    if (f == 4) c.Rect(0, 0, 23, 23, Rgba.Transparent).Px(6, 6, Pal.Slate).Px(17, 15, Pal.Slate);
                }
                break;
            case "game-ready":
                s.Tags.Add(new Tag { Name = "idle", From = 0, To = 1 });
                s.Tags.Add(new Tag { Name = "walk", From = 2, To = 5 });
                for (int f = 1; f < s.Frames.Count; f++) { c.NewFrame(); s.GetOrCreateCel(work, f).Image.Pixels.CopyTo(c.Image.Pixels, 0); }
                break;
            default: throw new Xunit.Sdk.XunitException("Write a passing answer for the new exercise '" + id + "' here.");
        }
        for (int f = 0; f < c.Frames.Count; f++)
        {
            while (s.Frames.Count <= f) s.Frames.Add(new Frame());
            c.Frames[f].Pixels.CopyTo(s.GetOrCreateCel(work, f).Image.Pixels, 0);
        }
        var r = e.Check(s);
        Assert.True(r.Passed, id + ": " + string.Join(" | ", r.Lines.Where(l => !l.Ok).Select(l => l.English)));
    }

    [Fact]
    public void Check_IgnoresTheGuideLayer()
    {
        var e = Exercises.Get("first-sprite");
        var s = e.CreateDocument();
        Assert.Contains(s.Cels[(s.Layers[0], 0)].Image.Pixels, p => p != 0); // the guide has marks
        var input = Exercise.Input(s);
        Assert.All(input.Work.Pixels, p => Assert.Equal(0u, p));
        Assert.Contains(input.Guide.Pixels, p => p != 0);
    }


    /// <summary>The guide is found by its mark, not its name: renaming either layer changes nothing, and the names can be translated.</summary>
    [Fact]
    public void Guide_IsFoundByItsMark_NotItsName()
    {
        var e = Exercises.Get("first-sprite");
        var s = e.CreateDocument("Guia", "Desenho");
        Assert.Equal(("Guia", "Desenho"), (s.Layers[0].Name, s.Layers[1].Name));
        Assert.True(Exercise.IsGuide(s, s.Layers[0]));
        Assert.False(Exercise.IsGuide(s, s.Layers[1]));

        s.Layers[0].Name = "My reference";
        s.Layers[1].Name = "Guide"; // the old guide name on the drawing layer doesn't make it a guide
        Assert.True(Exercise.IsGuide(s, s.Layers[0]));
        Assert.False(Exercise.IsGuide(s, s.Layers[1]));
        Assert.All(Exercise.Input(s).Work.Pixels, p => Assert.Equal(0u, p)); // the guide's marks still don't count

        // .epx keeps the mark.
        var ms = new MemoryStream();
        new EpxFormat().Write(s, ms, new List<string>());
        ms.Position = 0;
        var back = new EpxFormat().Read(ms, new List<string>());
        Assert.True(Exercise.IsGuide(back, back.Layers[0]));
        Assert.False(Exercise.IsGuide(back, back.Layers[1]));
    }

    /// <summary>A practice saved before the mark existed (or as .aseprite, which drops it) still finds its layer named "Guide".</summary>
    [Fact]
    public void Guide_WithoutAMark_FallsBackToTheName()
    {
        var s = Exercises.Get("first-sprite").CreateDocument();
        s.Layers[0].Extra.Remove(Exercise.GuideKey);
        Assert.True(Exercise.IsGuide(s, s.Layers[0]));
        Assert.False(Exercise.IsGuide(s, s.Layers[1]));
        Assert.All(Exercise.Input(s).Work.Pixels, p => Assert.Equal(0u, p));
        s.Layers[0].Name = "Renamed";
        Assert.False(Exercise.IsGuide(s, s.Layers[0])); // no mark and not named Guide: an ordinary layer
    }

    [Fact]
    public void ShadeBall_StartsFlat_AndPillowShadingFails()
    {
        var e = Exercises.Get("shade-ball");
        var s = e.CreateDocument();
        var cel = s.Cels[(s.Layers[1], 0)];
        // Concentric rings, dark outside and light in the middle: pillow shading.
        var sink = new PixelSink(cel.Image);
        Shapes.Ellipse(sink, Pal.Plum.Packed, 4, 4, 27, 27, new Brush(), true);
        Shapes.Ellipse(sink, Pal.Red.Packed, 7, 7, 24, 24, new Brush(), true);
        Shapes.Ellipse(sink, Pal.Orange.Packed, 11, 11, 20, 20, new Brush(), true);
        var r = e.Check(s);
        Assert.False(r.Passed);
        Assert.Contains(r.Lines, l => !l.Ok && l.Text.Contains("pillow"));
    }
}
