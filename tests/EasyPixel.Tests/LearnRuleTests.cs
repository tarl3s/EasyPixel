using EasyPixel.Core.Learn;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

/// <summary>Each check rule with a drawing that passes and one that fails.</summary>
public class LearnRuleTests
{
    private static CheckInput Draw(int w, int h, Action<ArtCanvas> draw)
    {
        var c = new ArtCanvas(w, h);
        draw(c);
        return CheckInput.Of(c.Frames.ToArray());
    }

    private static void Passes(CheckRule rule, CheckInput input)
    {
        var line = rule.Check(input);
        Assert.True(line.Ok, line.English);
    }

    private static void Fails(CheckRule rule, CheckInput input)
    {
        var line = rule.Check(input);
        Assert.False(line.Ok, "expected a failure, got: " + line.English);
    }

    private static readonly CheckInput Empty = Draw(16, 16, _ => { });
    private static readonly CheckInput Ball = Draw(32, 32, c => c.Sphere(4, 4, 27, 27, new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow }));

    [Fact]
    public void NotEmpty()
    {
        Fails(Rules.NotEmpty(), Empty);
        Fails(Rules.NotEmpty(10), Draw(16, 16, c => c.Px(1, 1, Pal.Red)));
        Passes(Rules.NotEmpty(10), Draw(16, 16, c => c.Rect(0, 0, 3, 3, Pal.Red)));
    }

    [Fact]
    public void ColorCounts()
    {
        var three = Draw(8, 8, c => c.Px(0, 0, Pal.Red).Px(1, 0, Pal.Blue).Px(2, 0, Pal.Green));
        Passes(Rules.MaxColors(3), three);
        Fails(Rules.MaxColors(2), three);
        Passes(Rules.MinColors(3), three);
        Fails(Rules.MinColors(4), three);
    }

    [Fact]
    public void KeepsMargin()
    {
        Passes(Rules.KeepsMargin(1), Draw(8, 8, c => c.Rect(1, 1, 6, 6, Pal.Red)));
        Fails(Rules.KeepsMargin(1), Draw(8, 8, c => c.Rect(1, 1, 7, 6, Pal.Red)));
    }

    [Fact]
    public void FillRatio()
    {
        var quarter = Draw(10, 10, c => c.Rect(0, 0, 4, 4, Pal.Red));
        Passes(Rules.FillRatio(0.2, 0.3), quarter);
        Fails(Rules.FillRatio(0.3, 0.6), quarter);
        Fails(Rules.FillRatio(0.1, 0.2), quarter);
    }

    [Fact]
    public void MaxParts()
    {
        var shape = Draw(10, 10, c => c.Rect(1, 1, 5, 5, Pal.Red).Px(6, 6, Pal.Red)); // touching diagonally = one piece
        Passes(Rules.MaxParts(1), shape);
        var stray = Draw(10, 10, c => c.Rect(1, 1, 5, 5, Pal.Red).Px(8, 8, Pal.Red));
        Fails(Rules.MaxParts(1), stray);
        Assert.Contains(8, Rules.MaxParts(1).Check(stray).Args); // points at the stray pixel
    }

    [Fact]
    public void NoClumps()
    {
        Passes(Rules.NoClumps(), Draw(10, 10, c => c.Line(0, 0, 9, 4, Pal.Black)));
        Fails(Rules.NoClumps(), Draw(10, 10, c => c.Line(0, 0, 9, 4, Pal.Black).Rect(3, 3, 4, 4, Pal.Black)));
    }

    [Fact]
    public void PixelPerfect()
    {
        Passes(Rules.PixelPerfect(), Draw(16, 16, c => c.Line(0, 15, 15, 5, Pal.Black)));
        Passes(Rules.PixelPerfect(), Draw(16, 16, c => c.Rect(2, 2, 12, 12, Pal.Black, filled: false))); // square corners are fine
        Passes(Rules.PixelPerfect(), Draw(16, 16, c => c.Rect(2, 2, 12, 12, Pal.Black)));                 // so are filled shapes
        var stairs = Draw(16, 16, c => { for (int i = 0; i < 5; i++) c.Px(i * 2, 10 - i, Pal.Black).Px(i * 2 + 1, 10 - i, Pal.Black).Px(i * 2 + 1, 9 - i, Pal.Black); });
        Fails(Rules.PixelPerfect(), stairs);
        Passes(Rules.PixelPerfect(allowed: 99), stairs);
    }

    [Fact]
    public void HasOutline()
    {
        var bare = Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red));
        Fails(Rules.HasOutline(), bare);
        Passes(Rules.HasOutline(), Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red).Outline(Pal.Black)));
        Passes(Rules.HasOutline(Pal.Black), Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red).Outline(Pal.Black)));
        Fails(Rules.HasOutline(Pal.Black), Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red).Outline(Pal.Plum)));
        Passes(Rules.HasOutline(), Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red).SelOut()));
        Fails(Rules.HasOutline(), Draw(16, 16, c => c.Ellipse(3, 3, 12, 12, Pal.Red).Outline(Pal.White))); // a lighter rim isn't an outline
        var gap = Draw(16, 16, c => { c.Ellipse(3, 3, 12, 12, Pal.Red).Outline(Pal.Black); c.Line(5, 2, 10, 2, Pal.Red); c.Line(5, 1, 10, 1, Pal.Red); });
        Fails(Rules.HasOutline(), gap);
        Fails(Rules.HasOutline(), Empty);
        Fails(Rules.HasOutline(), Draw(16, 16, c => c.Line(0, 0, 15, 15, Pal.Black))); // a line has no fill to outline
    }

    [Fact]
    public void LightFrom()
    {
        Passes(Rules.LightFrom(LightDir.TopLeft), Ball);
        Fails(Rules.LightFrom(LightDir.Right), Ball);
        var pillow = Draw(32, 32, c => c.Ellipse(2, 2, 29, 29, Pal.Plum).Ellipse(6, 6, 25, 25, Pal.Red).Ellipse(11, 11, 20, 20, Pal.Orange));
        var line = Rules.LightFrom().Check(pillow);
        Assert.False(line.Ok);
        Assert.Contains("pillow", line.Text);
        var backwards = Draw(32, 32, c => c.Sphere(4, 4, 27, 27, new[] { Pal.Plum, Pal.Red, Pal.Orange, Pal.Yellow }, 1, 1));
        Assert.Contains("backwards", Rules.LightFrom().Check(backwards).Text);
        Fails(Rules.LightFrom(), Draw(32, 32, c => c.Px(3, 3, Pal.Red)));
    }

    [Fact]
    public void FullyOpaque()
    {
        Passes(Rules.FullyOpaque(), Draw(4, 4, c => c.Rect(0, 0, 3, 3, Pal.Green)));
        Fails(Rules.FullyOpaque(), Draw(4, 4, c => c.Rect(0, 0, 3, 2, Pal.Green)));
    }

    [Fact]
    public void Seamless()
    {
        var periodic = Draw(16, 16, c => { for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) c.Px(x, y, ((x + 2 * y) % 4) switch { 0 => Pal.Lime, 1 => Pal.Green, _ => Pal.Teal }); });
        Passes(Rules.Seamless(), periodic);
        var flat = Draw(16, 16, c => c.Rect(0, 0, 15, 15, Pal.Green));
        Passes(Rules.Seamless(), flat);
        var gradient = Draw(16, 16, c => { for (int x = 0; x < 16; x++) c.Line(x, 0, x, 15, Pal.Mix(Pal.Navy, Pal.Yellow, x / 15.0)); });
        Fails(Rules.Seamless(horizontal: true, vertical: false), gradient);
        Passes(Rules.Seamless(horizontal: false, vertical: true), gradient);
        var edgeStripe = Draw(16, 16, c => c.Rect(0, 0, 15, 15, Pal.Green).Rect(0, 15, 15, 15, Pal.Black)); // a dark bottom row only
        Fails(Rules.Seamless(), edgeStripe);
    }

    [Fact]
    public void HasFrames()
    {
        var a = new ArtCanvas(8, 8).Px(1, 1, Pal.Red);
        a.NewFrame().Px(2, 1, Pal.Red);
        a.NewFrame(copy: true);
        var input = CheckInput.Of(a.Frames.ToArray());
        Passes(Rules.HasFrames(3, movement: false), input);
        Fails(Rules.HasFrames(3), input); // frames 2 and 3 are the same
        Fails(Rules.HasFrames(4, movement: false), input);
        Passes(Rules.HasFrames(2), CheckInput.Of(a.Frames.Take(2).ToArray()));
    }

    [Fact]
    public void RampAndHueShift()
    {
        var pts = new[] { (0, 0), (2, 0), (4, 0), (6, 0) };
        var good = Draw(8, 1, c => c.Px(0, 0, Pal.Navy).Px(2, 0, Pal.Red).Px(4, 0, Pal.Orange).Px(6, 0, Pal.Yellow));
        Passes(Rules.RampOrdered(pts), good);
        Passes(Rules.HueShift((0, 0), (6, 0)), good);
        var unordered = Draw(8, 1, c => c.Px(0, 0, Pal.Navy).Px(2, 0, Pal.Orange).Px(4, 0, Pal.Red).Px(6, 0, Pal.Yellow));
        Fails(Rules.RampOrdered(pts), unordered);
        Fails(Rules.RampOrdered(pts), Draw(8, 1, c => c.Px(0, 0, Pal.Navy)));
        var flat = Draw(8, 1, c => c.Px(0, 0, Pal.Mix(Pal.Hex("000000"), Pal.Orange, 0.3)).Px(6, 0, Pal.Orange));
        Fails(Rules.HueShift((0, 0), (6, 0)), flat); // only darker, same hue
        Fails(Rules.HueShift((0, 0), (6, 0)), Draw(8, 1, c => c.Px(0, 0, Pal.Hex("202020")).Px(6, 0, Pal.Hex("e0e0e0"))));
    }

    [Fact]
    public void UsesDither()
    {
        Passes(Rules.UsesDither(), Draw(8, 8, c => { for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) c.Px(x, y, (x + y) % 2 == 0 ? Pal.Navy : Pal.Blue); }));
        Fails(Rules.UsesDither(), Draw(8, 8, c => c.Rect(0, 0, 3, 7, Pal.Navy).Rect(4, 0, 7, 7, Pal.Blue)));
    }

    [Fact]
    public void Contrast()
    {
        var wide = Draw(4, 1, c => c.Px(0, 0, Pal.Black).Px(1, 0, Pal.White));
        var narrow = Draw(4, 1, c => c.Px(0, 0, Pal.Red).Px(1, 0, Pal.Blue)); // different hues, almost the same value
        Passes(Rules.Contrast(min: 150), wide);
        Fails(Rules.Contrast(min: 150), narrow);
        Passes(Rules.Contrast(max: 70), narrow);
        Fails(Rules.Contrast(max: 70), wide);
        Fails(Rules.Contrast(min: 10), Empty);
    }

    private static CheckInput AaCurve(Action<ArtCanvas>? extra = null) => Draw(32, 32, c => { Exercises.AaStart(c); extra?.Invoke(c); });

    [Fact]
    public void AntiAliased()
    {
        var rule = Rules.AntiAliased(Pal.Black, Pal.White, 6);
        Fails(rule, AaCurve()); // no in-between pixels yet
        Passes(rule, AaCurve(c => LessonArt.AntiAlias(c, Pal.Black, 28, 28, 23, Pal.Slate, Pal.Silver)));
        // In-between pixels along the straight bottom side: the straight part doesn't need them.
        var line = rule.Check(AaCurve(c => { for (int x = 10; x <= 25; x++) c.Px(x, 27, Pal.Silver); }));
        Assert.False(line.Ok);
        Assert.Contains("straight", line.Text);
        // A pixel away from the line, and a colour outside the line–background range.
        Fails(rule, AaCurve(c => { LessonArt.AntiAlias(c, Pal.Black, 28, 28, 23, Pal.Slate, Pal.Silver); c.Px(18, 18, Pal.Silver); }));
        Fails(Rules.AntiAliased(Pal.Black, Pal.Silver, 1), Draw(8, 8, c => c.Rect(0, 0, 7, 7, Pal.Silver).Line(0, 7, 7, 0, Pal.Black).Px(1, 7, Pal.White)));
        Fails(rule, Draw(32, 32, c => c.Rect(0, 0, 31, 31, Pal.White))); // the line is gone
    }

    [Fact]
    public void GetsLighter()
    {
        var down = Draw(8, 8, c => { for (int y = 0; y < 8; y++) c.Line(0, y, 7, y, Pal.Mix(Pal.Navy, Pal.Aqua, y / 7.0)); });
        Passes(Rules.GetsLighter(Toward.Down), down);
        Fails(Rules.GetsLighter(Toward.Up), down);
        Fails(Rules.GetsLighter(Toward.Right), down); // flat sideways
        var dip = Draw(8, 8, c => { for (int y = 0; y < 8; y++) c.Line(0, y, 7, y, y is 4 or 5 ? Pal.Black : Pal.Mix(Pal.Navy, Pal.Aqua, y / 7.0)); });
        Fails(Rules.GetsLighter(Toward.Down), dip);
        Fails(Rules.GetsLighter(Toward.Down), Draw(8, 8, c => c.Rect(0, 0, 7, 3, Pal.Navy))); // half empty
    }

    [Fact]
    public void Texture()
    {
        var flat = Draw(8, 8, c => c.Rect(0, 0, 7, 7, Pal.Slate));
        var noise = Draw(8, 8, c => { for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) c.Px(x, y, (x + y) % 2 == 0 ? Pal.Slate : Pal.Silver); });
        var bricks = Draw(8, 8, c => c.Rect(0, 0, 7, 7, Pal.Red).Line(0, 3, 7, 3, Pal.Plum).Line(0, 7, 7, 7, Pal.Plum).Line(3, 0, 3, 2, Pal.Plum).Line(6, 4, 6, 6, Pal.Plum));
        Fails(Rules.Texture(0.1, 0.45), flat);
        Fails(Rules.Texture(0.1, 0.45), noise);
        Passes(Rules.Texture(0.1, 0.45), bricks);
        Fails(Rules.Texture(0.1, 0.45), Empty);
    }

    private static CheckInput Frames(int w, int h, params Action<ArtCanvas>[] frames)
    {
        var c = new ArtCanvas(w, h);
        for (int i = 0; i < frames.Length; i++)
        {
            if (i > 0) c.NewFrame();
            frames[i](c);
        }
        return CheckInput.Of(c.Frames.ToArray());
    }

    [Fact]
    public void Bounces()
    {
        static Action<ArtCanvas> Ball(int bottom) => c => c.Ellipse(10, bottom - 7, 17, bottom, Pal.Red);
        var rule = Rules.Bounces(29, 10);
        Passes(rule, Frames(32, 32, Ball(29), Ball(20), Ball(14), Ball(20)));
        Fails(rule, Frames(32, 32, Ball(25), Ball(15), Ball(10)));        // never lands
        Fails(rule, Frames(32, 32, Ball(29), Ball(26), Ball(24)));        // barely leaves the floor
        Fails(rule, Frames(32, 32, Ball(31), Ball(15)));                  // sinks through the floor
        Fails(rule, Frames(32, 32, Ball(29), _ => { }));                  // an empty frame
    }

    [Fact]
    public void OnFloor_InPlace_Bobs()
    {
        static Action<ArtCanvas> Body(int x, int top, int bottom) => c => c.Rect(x, top, x + 5, bottom, Pal.Blue);
        var walk = Frames(24, 32, Body(9, 10, 29), Body(9, 9, 29), Body(10, 10, 29), Body(9, 9, 29));
        Passes(Rules.OnFloor(29), walk);
        Passes(Rules.InPlace(3), walk);
        Passes(Rules.Bobs(1), walk);
        Fails(Rules.OnFloor(29), Frames(24, 32, Body(9, 10, 29), Body(9, 8, 27)));   // floats
        Fails(Rules.OnFloor(29), Frames(24, 32, Body(9, 10, 30)));                    // sinks
        Fails(Rules.OnFloor(29), Frames(24, 32, Body(9, 10, 29), _ => { }));          // an empty frame
        Fails(Rules.InPlace(3), Frames(24, 32, Body(2, 10, 29), Body(8, 10, 29), Body(14, 10, 29)));
        Fails(Rules.Bobs(1), Frames(24, 32, Body(9, 10, 29), Body(10, 10, 29)));      // the same height
        Fails(Rules.Bobs(1), Frames(24, 32, Body(9, 10, 29)));                        // one frame
        Fails(Rules.InPlace(3), Empty);
    }

    [Fact]
    public void Repeats()
    {
        static void Window(ArtCanvas c, int x, int w = 4) => c.Rect(x, 2, x + w - 1, 6, Pal.Sky);
        var copied = Draw(40, 10, c => { c.Rect(0, 0, 39, 9, Pal.Red); for (int i = 0; i < 4; i++) Window(c, 2 + i * 9); });
        Passes(Rules.Repeats(3), copied);
        var uneven = Draw(40, 10, c => { c.Rect(0, 0, 39, 9, Pal.Red); Window(c, 2); Window(c, 11); Window(c, 20, 5); Window(c, 30, 3); });
        Fails(Rules.Repeats(3), uneven); // only two match
        Assert.Contains("Only", Rules.Repeats(3).Check(uneven).Text);
        Fails(Rules.Repeats(3), Draw(40, 10, c => c.Rect(0, 0, 39, 9, Pal.Red)));
    }

    [Fact]
    public void UsesTheSpace()
    {
        Passes(Rules.UsesTheSpace(0.7), Draw(16, 16, c => c.Ellipse(2, 3, 13, 12, Pal.Yellow)));
        Fails(Rules.UsesTheSpace(0.7), Draw(16, 16, c => c.Ellipse(5, 5, 9, 9, Pal.Yellow)));
        Fails(Rules.UsesTheSpace(0.7), Empty);
    }

    [Fact]
    public void GrowsThenFades()
    {
        static Action<ArtCanvas> Disc(int r) => c => { if (r > 0) c.Ellipse(12 - r, 12 - r, 12 + r, 12 + r, Pal.Orange); };
        Passes(Rules.GrowsThenFades(), Frames(24, 24, Disc(1), Disc(5), Disc(9), Disc(4), Disc(1)));
        Fails(Rules.GrowsThenFades(), Frames(24, 24, Disc(9), Disc(5), Disc(1)));       // starts at its biggest
        Fails(Rules.GrowsThenFades(), Frames(24, 24, Disc(1), Disc(5), Disc(9)));       // never fades
        Fails(Rules.GrowsThenFades(), Frames(24, 24, Disc(1), Disc(5)));                // too short
        Fails(Rules.GrowsThenFades(), Frames(24, 24, Disc(0), Disc(0), Disc(0)));       // empty
    }

    [Fact]
    public void HasTags()
    {
        var input = Frames(8, 8, c => c.Px(1, 1, Pal.Red), c => c.Px(2, 1, Pal.Red), c => c.Px(3, 1, Pal.Red), c => c.Px(4, 1, Pal.Red));
        var tags = input.Sprite.Tags;
        Fails(Rules.HasTags(2), input);
        tags.Add(new Tag { Name = "idle", From = 0, To = 1 });
        Fails(Rules.HasTags(2), input);
        tags.Add(new Tag { Name = "walk", From = 2, To = 2 });
        Assert.Contains("isn't in any tag", Rules.HasTags(2).Check(input).Text); // frame 4 is left out
        tags[1].To = 3;
        Passes(Rules.HasTags(2), input);
        tags[1].Name = "Idle";
        Fails(Rules.HasTags(2), input); // two tags with one name
        tags[1].Name = " ";
        Fails(Rules.HasTags(2), input);
    }

    [Fact]
    public void FilledBottom()
    {
        Passes(Rules.FilledBottom(), Draw(8, 8, c => c.Rect(0, 4, 7, 7, Pal.Teal)));
        Fails(Rules.FilledBottom(), Draw(8, 8, c => c.Rect(0, 4, 6, 7, Pal.Teal)));
    }

    [Fact]
    public void CheckLine_English_FillsPlaceholders()
    {
        var line = Rules.MaxColors(1).Check(Draw(4, 1, c => c.Px(0, 0, Pal.Red).Px(1, 0, Pal.Blue)));
        Assert.Equal("You used 2 colours; the limit is 1. Merge the shades that look almost the same.", line.English);
    }
}
