using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Core.Select;

namespace EasyPixel.Tests;

public class RotateTests
{
    private static readonly uint R = new Rgba(255, 0, 0, 255).Packed, B = new Rgba(0, 0, 255, 255).Packed;

    [Fact]
    public void QuarterTurns_AreExact()
    {
        var img = new Image(3, 2);
        img.Set(0, 0, R);
        Assert.Equal(Transform.Rotate90(img).Pixels, Transform.RotateNearest(img, 90).Pixels);
        Assert.Equal(Transform.Rotate90(img).Pixels, Transform.RotateNearest(img, -270).Pixels);
        Assert.Equal(img.Pixels, Transform.RotateNearest(img, 360).Pixels);
    }

    [Fact]
    public void FortyFive_GrowsTheBox_KeepsOnlySourceColours_NoBlending()
    {
        var img = new Image(10, 10, B);
        for (int x = 0; x < 10; x++) img.Set(x, 0, R);
        var r = Transform.RotateNearest(img, 45);
        Assert.InRange(r.Width, 14, 15);
        Assert.Equal(r.Width, r.Height);
        Assert.All(r.Pixels, p => Assert.True(p == 0 || p == R || p == B)); // no new colours
        Assert.Equal(0u, r.Get(0, 0)); // the corners nothing lands on stay clear
        Assert.Contains(R, r.Pixels);
    }

    [Fact]
    public void Floating_RotateBy_StaysCentred_AndUsesItsClearValue()
    {
        var f = new Floating(new Image(10, 4, 7), 20, 30, clear: 7);
        f.RotateBy(30);
        double cx = f.X + f.Pixels.Width / 2.0, cy = f.Y + f.Pixels.Height / 2.0;
        Assert.InRange(cx, 24.5, 25.5);
        Assert.InRange(cy, 31.5, 32.5);
        Assert.All(f.Pixels.Pixels, p => Assert.Equal(7u, p));
    }
}
