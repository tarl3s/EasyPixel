using System.Diagnostics;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using Xunit;

namespace EasyPixel.Tests;

public class RasterTests
{
    private const uint Red = 0xFF0000FF, Blue = 0xFFFF0000, Green = 0xFF00FF00;

    private static HashSet<(int, int)> Lit(Image img, uint v = Red)
    {
        var set = new HashSet<(int, int)>();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (img.Get(x, y) == v) set.Add((x, y));
        return set;
    }

    // ---- shapes ----

    [Fact]
    public void Line_PixelPerfect_NoDoubleCorners()
    {
        var img = new Image(8, 8);
        Shapes.Line(new PixelSink(img), Red, 0, 0, 5, 2, new Brush(), pixelPerfect: true);
        var lit = Lit(img);
        Assert.Equal(new HashSet<(int, int)> { (0, 0), (1, 0), (2, 1), (3, 1), (4, 2), (5, 2) }, lit);
        foreach (var (x, y) in lit) // no pixel joins a horizontal and a vertical neighbour
            Assert.False((lit.Contains((x - 1, y)) || lit.Contains((x + 1, y))) && (lit.Contains((x, y - 1)) || lit.Contains((x, y + 1))));
    }

    [Fact]
    public void PixelPerfect_DropsTheCornerOfAnL()
    {
        var pts = new List<(int, int)> { (0, 0), (1, 0), (1, 1), (2, 1) };
        Assert.Equal(new List<(int, int)> { (0, 0), (1, 1), (2, 1) }, Shapes.PixelPerfect(pts));
    }

    [Fact]
    public void Line_NotPixelPerfect_KeepsCorner()
    {
        var img = new Image(4, 4);
        Shapes.Line(new PixelSink(img), Red, 0, 0, 1, 1, new Brush(), pixelPerfect: false);
        Assert.Equal(new HashSet<(int, int)> { (0, 0), (1, 1) }, Lit(img));
    }

    [Fact]
    public void Brush_Square3_Covers9_Circle3_IsAPlus()
    {
        var sq = new Image(5, 5);
        Brush.Stamp(new PixelSink(sq), 2, 2, Red, new Brush(3, BrushShape.Square));
        Assert.Equal(9, Lit(sq).Count);
        var ci = new Image(5, 5);
        Brush.Stamp(new PixelSink(ci), 2, 2, Red, new Brush(3, BrushShape.Circle));
        Assert.Equal(new HashSet<(int, int)> { (2, 1), (1, 2), (2, 2), (3, 2), (2, 3) }, Lit(ci));
    }

    [Fact]
    public void Sink_IgnoresOutOfBounds_AndTracksDirty()
    {
        var img = new Image(4, 4);
        var s = new PixelSink(img);
        Assert.True(s.Dirty.IsEmpty);
        s.Set(-1, 0, Red);
        s.Set(4, 4, Red);
        Assert.True(s.Dirty.IsEmpty);
        s.Set(1, 2, Red);
        s.Set(3, 1, Red);
        Assert.Equal(new PixelRect(1, 1, 3, 2), s.Dirty);
    }

    [Fact]
    public void Rect_Outline_And_Filled()
    {
        var img = new Image(6, 6);
        Shapes.Rect(new PixelSink(img), Red, 1, 1, 4, 3, new Brush(), filled: false);
        Assert.Equal(10, Lit(img).Count);
        Assert.False(img.Get(2, 2) == Red);
        var f = new Image(6, 6);
        Shapes.Rect(new PixelSink(f), Red, 4, 3, 1, 1, new Brush(), filled: true);
        Assert.Equal(12, Lit(f).Count);
    }

    [Fact]
    public void Ellipse_Filled_Symmetric()
    {
        var img = new Image(9, 7);
        Shapes.Ellipse(new PixelSink(img), Red, 0, 0, 8, 6, new Brush(), filled: true);
        var lit = Lit(img);
        Assert.True(lit.Count > 20);
        foreach (var (x, y) in lit)
        {
            Assert.Contains((8 - x, y), lit);
            Assert.Contains((x, 6 - y), lit);
        }
        Assert.Contains((4, 3), lit);
        Assert.DoesNotContain((0, 0), lit);
    }

    [Fact]
    public void Ellipse_Outline_HasHoleInside()
    {
        var img = new Image(11, 11);
        Shapes.Ellipse(new PixelSink(img), Red, 0, 0, 10, 10, new Brush(), filled: false);
        Assert.NotEqual(Red, img.Get(5, 5));
        Assert.Equal(Red, img.Get(5, 0));
        Assert.Equal(Red, img.Get(0, 5));
    }

    // ---- symmetry ----

    [Fact]
    public void Symmetry_HV_FourPoints()
    {
        var sym = new Symmetry { H = true, V = true, AxisX = 4, AxisY = 4 };
        var pts = sym.Mirror(1, 2).ToHashSet();
        Assert.Equal(new HashSet<(int, int)> { (1, 2), (6, 2), (1, 5), (6, 5) }, pts);
    }

    [Fact]
    public void Symmetry_PointOnAxis_NotDuplicated()
    {
        var sym = new Symmetry { H = true, AxisX = 3.5f };
        Assert.Single(sym.Mirror(3, 0));
    }

    [Fact]
    public void Shapes_AcceptSymmetry()
    {
        var img = new Image(8, 8);
        var sym = new Symmetry { H = true, V = true, AxisX = 4, AxisY = 4 };
        Shapes.Line(new PixelSink(img), Red, 0, 0, 2, 0, new Brush(), false, sym);
        Assert.Equal(12, Lit(img).Count);
        Assert.Equal(Red, img.Get(7, 7));
    }

    // ---- fill ----

    [Fact]
    public void Flood_Contiguous_StopsAtBorder()
    {
        var img = new Image(5, 5);
        for (int y = 0; y < 5; y++) img.Set(2, y, Blue); // wall
        Fill.Flood(new PixelSink(img), 0, 0, Red, contiguous: true, tolerance: 0);
        Assert.Equal(10, Lit(img).Count);
        Assert.Equal(0u, img.Get(3, 0));
        Assert.Equal(Blue, img.Get(2, 2));
    }

    [Fact]
    public void Flood_Global_ReplacesAll()
    {
        var img = new Image(5, 5);
        for (int y = 0; y < 5; y++) img.Set(2, y, Blue);
        Fill.Flood(new PixelSink(img), 0, 0, Red, contiguous: false, tolerance: 0);
        Assert.Equal(20, Lit(img).Count);
        Assert.Equal(5, Lit(img, Blue).Count);
    }

    [Fact]
    public void Flood_Tolerance_IncludesSimilar()
    {
        var img = new Image(3, 1, 0xFF000000);
        img.Set(1, 0, 0xFF000005);
        img.Set(2, 0, 0xFF000050);
        Fill.Flood(new PixelSink(img), 0, 0, Red, true, tolerance: 10);
        Assert.Equal(Red, img.Get(1, 0));
        Assert.Equal(0xFF000050u, img.Get(2, 0));
    }

    [Fact]
    public void Flood_Serpentine_ReachesTheEnd()
    {
        // Walls on every other row, with the gap alternating sides: the fill must snake through all of it.
        var img = new Image(9, 9);
        for (int y = 1; y < 9; y += 2)
            for (int x = 0; x < 9; x++)
                if (x != (y % 4 == 1 ? 8 : 0)) img.Set(x, y, Blue);
        Fill.Flood(new PixelSink(img), 0, 0, Red, true, 0);
        Assert.Equal(9 * 5 + 4, Lit(img).Count);
        Assert.Equal(Red, img.Get(8, 8));
    }

    [Fact]
    public void Flood_4096Square_UnderOneSecond()
    {
        var img = new Image(4096, 4096);
        var sw = Stopwatch.StartNew();
        Fill.Flood(new PixelSink(img), 100, 100, Red, true, 0);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1000, $"took {sw.ElapsedMilliseconds} ms");
        Assert.Equal(Red, img.Get(4095, 4095));
        Assert.Equal(Red, img.Get(0, 0));
    }

    [Fact]
    public void Flood_RespectsSelectionClip()
    {
        var img = new Image(6, 6);
        var s = new PixelSink(img, (x, y) => x < 3);
        Fill.Flood(s, 0, 0, Red, contiguous: true, tolerance: 0);
        Assert.Equal(18, Lit(img).Count);
        Assert.Equal(0u, img.Get(3, 0));
        Fill.Flood(s, 5, 5, Green, true, 0); // start outside the clip: nothing happens
        Assert.Empty(Lit(img, Green));
        Assert.Equal(new PixelRect(0, 0, 3, 6), s.Dirty);
    }

    // ---- dither ----

    [Fact]
    public void Bayer4_Pattern_Matches()
    {
        int[,] expected = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                Assert.Equal(expected[y, x], Dither.Threshold(DitherKind.Bayer4, x, y));
                Assert.Equal(expected[y, x] < 8, Dither.Pattern(DitherKind.Bayer4, x, y));
                Assert.Equal(expected[y, x], Dither.Threshold(DitherKind.Bayer4, x + 4, y + 8)); // tiles
            }
    }

    [Fact]
    public void Dither_Kinds_HalfCoverage()
    {
        foreach (var k in Enum.GetValues<DitherKind>())
        {
            int n = Dither.Size(k) * 2, on = 0;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (Dither.Pattern(k, x, y)) on++;
            Assert.Equal(n * n / 2, on);
        }
        Assert.True(Dither.Pattern(DitherKind.Checker, 0, 0));
        Assert.False(Dither.Pattern(DitherKind.Checker, 1, 0));
    }

    // ---- shading ----

    [Fact]
    public void Shading_StepsAlongRamp_AndIgnoresOthers()
    {
        var ramp = new Palette();
        ramp.Add(new Rgba(10, 0, 0, 255));
        ramp.Add(new Rgba(20, 0, 0, 255));
        ramp.Add(new Rgba(30, 0, 0, 255));
        var img = new Image(3, 1);
        img.Set(0, 0, ramp.Colors[1].Packed);
        img.Set(1, 0, ramp.Colors[2].Packed);
        img.Set(2, 0, Blue);
        var s = new PixelSink(img);
        for (int x = 0; x < 3; x++) if (Shading.Step(img.Get(x, 0), ramp, lighten: true) is uint v) s.Set(x, 0, v);
        Assert.Equal(ramp.Colors[2].Packed, img.Get(0, 0));
        Assert.Equal(ramp.Colors[2].Packed, img.Get(1, 0)); // end of the ramp
        Assert.Equal(Blue, img.Get(2, 0));
        img.Set(0, 0, Shading.Step(img.Get(0, 0), ramp, lighten: false)!.Value);
        Assert.Equal(ramp.Colors[1].Packed, img.Get(0, 0));
    }

    [Fact]
    public void Shading_IndexedMode_UsesIndices()
    {
        var ramp = new Palette();
        for (int i = 0; i < 3; i++) ramp.Add(new Rgba((byte)i, 0, 0, 255));
        var img = new Image(1, 1, 1);
        Assert.Equal(2u, Shading.Step(img.Get(0, 0), ramp, lighten: true, indexed: true));
        Assert.Null(Shading.Step(1, ramp, lighten: true, indexed: true, transparentIndex: 2)); // never into the transparent entry
    }

    [Fact]
    public void LightenDarken_ChangesLightness_KeepsAlpha()
    {
        var img = new Image(2, 1);
        img.Set(0, 0, new Rgba(100, 100, 100, 255).Packed);
        var s = new PixelSink(img);
        Shading.LightenDarken(s, 0, 0, new Brush(), 20);
        var c = Rgba.FromPacked(img.Get(0, 0));
        Assert.InRange(c.R, 150, 153);
        Assert.Equal(255, c.A);
        Shading.LightenDarken(s, 1, 0, new Brush(), 20); // transparent stays
        Assert.Equal(0u, img.Get(1, 0));
    }

    // ---- outline ----

    [Fact]
    public void Outline_Outside_OnlyTransparentNeighbours()
    {
        var img = new Image(5, 5);
        img.Set(2, 2, Blue);
        Outline.Apply(new PixelSink(img), Red, outside: true, diagonal: false);
        Assert.Equal(new HashSet<(int, int)> { (2, 1), (1, 2), (3, 2), (2, 3) }, Lit(img));
        Assert.Equal(Blue, img.Get(2, 2));

        var d = new Image(5, 5);
        d.Set(2, 2, Blue);
        Outline.Apply(new PixelSink(d), Red, true, diagonal: true);
        Assert.Equal(8, Lit(d).Count);
    }

    [Fact]
    public void Outline_Inside_PaintsEdgeOfShape()
    {
        var img = new Image(5, 5);
        for (int y = 1; y < 4; y++)
            for (int x = 1; x < 4; x++) img.Set(x, y, Blue);
        Outline.Apply(new PixelSink(img), Red, outside: false, diagonal: false);
        Assert.Equal(Blue, img.Get(2, 2));
        Assert.Equal(8, Lit(img).Count);
    }

    // ---- transform ----

    private static Image Numbered(int w, int h)
    {
        var img = new Image(w, h);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (uint)(i + 1);
        return img;
    }

    [Fact]
    public void Rotate90_FourTimes_Identity()
    {
        var img = Numbered(5, 3);
        var r = img;
        for (int i = 0; i < 4; i++) r = Transform.Rotate90(r);
        Assert.Equal(img.Pixels, r.Pixels);
        var once = Transform.Rotate90(img);
        Assert.Equal((3, 5), (once.Width, once.Height));
        Assert.Equal(img.Get(0, 2), once.Get(0, 0)); // clockwise: bottom-left goes to top-left
        Assert.Equal(img.Pixels, Transform.Rotate180(Transform.Rotate180(img)).Pixels);
        Assert.Equal(Transform.Rotate180(img).Pixels, Transform.Rotate90(Transform.Rotate90(img)).Pixels);
    }

    [Fact]
    public void Flips_AreInvolutions()
    {
        var img = Numbered(4, 3);
        Assert.Equal(img.Pixels, Transform.FlipH(Transform.FlipH(img)).Pixels);
        Assert.Equal(img.Pixels, Transform.FlipV(Transform.FlipV(img)).Pixels);
        Assert.Equal(4u, Transform.FlipH(img).Get(0, 0));
        Assert.Equal(9u, Transform.FlipV(img).Get(0, 0));
    }

    [Fact]
    public void ScaleNearest_DoublesAndShrinks()
    {
        var img = Numbered(2, 2);
        var big = Transform.ScaleNearest(img, 4, 4);
        Assert.Equal(1u, big.Get(1, 1));
        Assert.Equal(4u, big.Get(3, 3));
        Assert.Equal(img.Pixels, Transform.ScaleNearest(big, 2, 2).Pixels);
    }

    // ---- pixelate ----

    [Fact]
    public void Pixelate_Cell4_BlockUniform()
    {
        var img = new Image(10, 9);
        var rng = new Random(7);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (uint)rng.Next(1, 4);
        Pixelate.Apply(new PixelSink(img), 4);
        for (int by = 0; by < 9; by += 4)
            for (int bx = 0; bx < 10; bx += 4)
            {
                uint v = img.Get(bx, by);
                for (int y = by; y < Math.Min(by + 4, 9); y++)
                    for (int x = bx; x < Math.Min(bx + 4, 10); x++) Assert.Equal(v, img.Get(x, y));
            }
    }

    [Fact]
    public void Pixelate_PicksMostCommonValue()
    {
        var img = new Image(2, 2, Red);
        img.Set(0, 0, Blue);
        Pixelate.Apply(new PixelSink(img), 2);
        Assert.Equal(4, Lit(img).Count);
    }

    // ---- quantize ----

    [Fact]
    public void MedianCut_MaxColorsRespected()
    {
        var img = new Image(32, 32);
        var rng = new Random(3);
        for (int i = 0; i < img.Pixels.Length; i++)
            img.Pixels[i] = new Rgba((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256), 255).Packed;
        foreach (int max in new[] { 1, 4, 16, 200 })
        {
            var p = Quantize.MedianCut(img, max);
            Assert.InRange(p.Colors.Count, 1, max);
        }
        Assert.Equal(16, Quantize.MedianCut(img, 16).Colors.Count);
    }

    [Fact]
    public void MedianCut_FewColors_ReturnsThemExactly_IgnoresTransparent()
    {
        var img = new Image(2, 2);
        img.Set(0, 0, Red);
        img.Set(1, 0, Blue);
        img.Set(0, 1, Red);
        var p = Quantize.MedianCut(img, 8);
        Assert.Equal(2, p.Colors.Count);
        Assert.Contains(Rgba.FromPacked(Red), p.Colors);
        Assert.Contains(Rgba.FromPacked(Blue), p.Colors);
    }

    [Fact]
    public void ToPalette_MapsNearest_KeepsTransparent_DoesNotTouchInput()
    {
        var pal = new Palette();
        pal.Add(new Rgba(0, 0, 0, 255));
        pal.Add(new Rgba(255, 255, 255, 255));
        var img = new Image(3, 1);
        img.Set(0, 0, new Rgba(30, 30, 30, 255).Packed);
        img.Set(1, 0, new Rgba(220, 220, 220, 255).Packed);
        var res = Quantize.ToPalette(img, pal, dither: false);
        Assert.Equal(pal.Colors[0].Packed, res.Get(0, 0));
        Assert.Equal(pal.Colors[1].Packed, res.Get(1, 0));
        Assert.Equal(0u, res.Get(2, 0));
        Assert.Equal(new Rgba(30, 30, 30, 255).Packed, img.Get(0, 0));
    }

    [Fact]
    public void ToPalette_Dither_MixesBothColorsOnMidGrey()
    {
        var pal = new Palette();
        pal.Add(new Rgba(0, 0, 0, 255));
        pal.Add(new Rgba(255, 255, 255, 255));
        var img = new Image(16, 16, new Rgba(128, 128, 128, 255).Packed);
        var res = Quantize.ToPalette(img, pal, dither: true);
        int white = res.Pixels.Count(p => p == pal.Colors[1].Packed);
        Assert.InRange(white, 100, 156);
        Assert.All(res.Pixels, p => Assert.Contains(p, new[] { pal.Colors[0].Packed, pal.Colors[1].Packed }));
    }
}
