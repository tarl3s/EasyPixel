using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using Xunit;

namespace EasyPixel.Tests;

public class SelectionTests
{
    [Fact]
    public void Lasso_Concave_ContainsInterior()
    {
        // A "U" shape: the notch between the arms is outside, the arms and the base are inside.
        var poly = new List<(int, int)> { (0, 0), (2, 0), (2, 5), (7, 5), (7, 0), (9, 0), (9, 8), (0, 8) };
        var s = Selectors.Lasso(10, 10, poly);
        Assert.True(s.Contains(1, 3));
        Assert.True(s.Contains(8, 3));
        Assert.True(s.Contains(4, 7));
        Assert.False(s.Contains(4, 2));
        Assert.False(s.Contains(5, 9));
        Assert.True(s.Contains(0, 0)); // outline included
        Assert.True(s.Contains(9, 8));
    }

    [Fact]
    public void MagicWand_Tolerance0_ExactColor()
    {
        var img = new Image(4, 1);
        img.Pixels[0] = 0xFF0000FF; img.Pixels[1] = 0xFF0000FF; img.Pixels[2] = 0xFF0000FE; img.Pixels[3] = 0xFF0000FF;
        var s = Selectors.MagicWand(img, 0, 0, 0, contiguous: true);
        Assert.True(s.Contains(0, 0) && s.Contains(1, 0));
        Assert.False(s.Contains(2, 0));
        Assert.False(s.Contains(3, 0));
        var g = Selectors.MagicWand(img, 0, 0, 0, contiguous: false);
        Assert.True(g.Contains(3, 0));
        Assert.False(g.Contains(2, 0));
        var t = Selectors.MagicWand(img, 0, 0, 2, contiguous: true);
        Assert.True(t.Contains(3, 0));
    }

    [Fact]
    public void Subtract_RemovesArea()
    {
        var a = Selectors.Rect(10, 10, new PixelRect(0, 0, 6, 6));
        var b = Selectors.Rect(10, 10, new PixelRect(3, 3, 6, 6));
        var d = a.Subtract(b);
        Assert.True(d.Contains(0, 0));
        Assert.False(d.Contains(3, 3));
        Assert.False(d.Contains(7, 7));
        Assert.Equal(new PixelRect(0, 0, 6, 6), d.Bounds);
        Assert.Equal(new PixelRect(3, 3, 3, 3), a.Intersect(b).Bounds);
        Assert.Equal(new PixelRect(0, 0, 9, 9), a.Union(b).Bounds);
        Assert.True(a.Invert().Contains(9, 9));
        Assert.True(Selection.None(3, 3).IsEmpty);
        Assert.Throws<ArgumentException>(() => a.Union(Selection.None(3, 3)));
    }

    [Fact]
    public void Ellipse_InscribedInRect()
    {
        var s = Selectors.Ellipse(10, 10, new PixelRect(0, 0, 9, 9));
        Assert.True(s.Contains(4, 4));
        Assert.False(s.Contains(0, 0));
    }

    [Fact]
    public void LiftMoveDrop_MovesPixels_LeavesTransparent()
    {
        var cel = new Cel(new Image(8, 8), 0, 0);
        cel.Image.Set(2, 2, 0xFF0000FF);
        cel.Image.Set(3, 2, 0xFF00FF00);
        var sel = Selectors.Rect(8, 8, new PixelRect(2, 2, 2, 1));
        var f = Floating.Lift(cel, sel, out var lifted);
        Assert.Equal(new PixelRect(2, 2, 2, 1), lifted);
        Assert.Equal(0u, cel.Image.Get(2, 2));
        Assert.Equal(0u, cel.Image.Get(3, 2));
        f.X += 3; f.Y += 2;
        f.Drop(cel);
        Assert.Equal(0xFF0000FFu, cel.Image.Get(5, 4));
        Assert.Equal(0xFF00FF00u, cel.Image.Get(6, 4));
        Assert.Equal(0u, cel.Image.Get(2, 2));
    }

    [Fact]
    public void Lift_RespectsCelOffset_AndDropGrowsCel()
    {
        var cel = new Cel(new Image(2, 2, 0xFF0000FF), 5, 5);
        var f = Floating.Lift(cel, Selectors.Rect(20, 20, new PixelRect(5, 5, 1, 1)), out _);
        Assert.Equal(1, f.Pixels.Width);
        Assert.Equal(0xFF0000FFu, f.Pixels.Get(0, 0));
        Assert.Equal(0u, cel.Image.Get(0, 0));
        f.X = 2; f.Y = 12;
        var r = f.Drop(cel);
        Assert.Equal(2, cel.X); Assert.Equal(5, cel.Y);
        Assert.Equal(5, cel.Image.Width); Assert.Equal(8, cel.Image.Height);
        Assert.Equal(0xFF0000FFu, cel.Image.Get(0, 7));   // dropped at canvas (2,12)
        Assert.Equal(0xFF0000FFu, cel.Image.Get(4, 1));   // old (1,1)... canvas (6,6)
        Assert.Equal(new PixelRect(0, 7, 1, 1), r);
    }

    [Fact]
    public void Floating_FlipH()
    {
        var f = new Floating(new Image(3, 1), 4, 4);
        f.Pixels.Set(0, 0, 1); f.Pixels.Set(2, 0, 2);
        f.FlipH();
        Assert.Equal(2u, f.Pixels.Get(0, 0));
        Assert.Equal(1u, f.Pixels.Get(2, 0));
        f.Rotate90();
        Assert.Equal(1, f.Pixels.Width); Assert.Equal(3, f.Pixels.Height);
        f.Scale(2, 6);
        Assert.Equal(6, f.Pixels.Height);
        f.FlipV();
    }
}
