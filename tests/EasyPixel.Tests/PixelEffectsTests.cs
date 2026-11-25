using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tests;

public class PixelEffectsTests
{
    private static readonly uint R = new Rgba(255, 0, 0, 255).Packed, B = new Rgba(0, 0, 255, 255).Packed;

    private static Image Halves()
    {
        var img = new Image(8, 8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) img.Set(x, y, x < 4 ? R : B);
        return img;
    }

    [Fact]
    public void Jumble_OnlyMovesPixels_NoColourAddedOrLost()
    {
        var img = Halves();
        var sink = new PixelSink(img);
        var rnd = new Random(3);
        for (int i = 0; i < 20; i++) for (int y = 0; y < 8; y++) for (int x = 2; x < 6; x++) PixelEffects.JumbleAt(sink, x, y, 100, rnd);
        Assert.Equal(32, img.Pixels.Count(p => p == R));
        Assert.Equal(32, img.Pixels.Count(p => p == B));
        Assert.Contains(Enumerable.Range(0, 8), y => img.Get(4, y) == R || img.Get(3, y) == B); // the edge did mix
    }

    [Fact]
    public void Jumble_RespectsTheClip_AndZeroPercentDoesNothing()
    {
        var img = Halves();
        var before = (uint[])img.Pixels.Clone();
        var sink = new PixelSink(img, (x, _) => x != 4); // column 4 can't be written
        var rnd = new Random(1);
        for (int y = 0; y < 8; y++) for (int i = 0; i < 50; i++) PixelEffects.JumbleAt(sink, 3, y, 100, rnd);
        for (int y = 0; y < 8; y++) Assert.Equal(B, img.Get(4, y));
        var img2 = Halves();
        var s2 = new PixelSink(img2);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) PixelEffects.JumbleAt(s2, x, y, 0, rnd);
        Assert.Equal(before, img2.Pixels);
    }

    [Fact]
    public void Blur_AtTheEdge_MixesTheColours_AwayFromItKeepsThem()
    {
        var img = Halves();
        Rgba? Read(int x, int y) => (uint)x < 8 && (uint)y < 8 ? Rgba.FromPacked(img.Get(x, y)) : null;
        var edge = PixelEffects.BlurAt(Read, 3, 4, 100);
        Assert.True(edge.R > 0 && edge.B > 0); // a purple-ish mix
        Assert.Equal(new Rgba(255, 0, 0, 255), PixelEffects.BlurAt(Read, 1, 4, 100));
        Assert.Equal(Rgba.FromPacked(R), PixelEffects.BlurAt(Read, 3, 4, 0)); // 0% leaves it
    }

    [Fact]
    public void Blur_NextToTransparency_FadesWithoutGoingBlack()
    {
        var img = new Image(3, 1);
        img.Set(0, 0, R);
        Rgba? Read(int x, int y) => (uint)x < 3 && y == 0 ? Rgba.FromPacked(img.Get(x, y)) : null;
        var c = PixelEffects.BlurAt(Read, 1, 0, 100);
        Assert.Equal(255, c.R); // still red, not darkened
        Assert.InRange(c.A, 1, 254); // partly see-through
    }
}
