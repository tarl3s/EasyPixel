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
    public void CheckLine_English_FillsPlaceholders()
    {
        var line = Rules.MaxColors(1).Check(Draw(4, 1, c => c.Px(0, 0, Pal.Red).Px(1, 0, Pal.Blue)));
        Assert.Equal("You used 2 colours; the limit is 1. Merge the shades that look almost the same.", line.English);
    }
}
