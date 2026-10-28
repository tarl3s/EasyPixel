using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public class ModelTests
{
    [Theory]
    [InlineData(8000, 1)]
    [InlineData(1, 8000)]
    public void NewSprite_AnySize(int w, int h)
    {
        var s = new Sprite(w, h);
        Assert.Equal(w, s.Width);
        Assert.Equal(h, s.Height);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-4, 8)]
    [InlineData(8, -4)]
    public void NewSprite_NonPositiveSize_Throws(int w, int h) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sprite(w, h));

    [Fact]
    public void Indexed_DrawColorNotInPalette_AddsEntry()
    {
        var s = new Sprite(4, 4, ColorMode.Indexed);
        int before = s.Palette.Colors.Count;
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        var c = new Rgba(10, 20, 30, 255);
        uint v = s.ColorToPixel(c);
        cel.Image.Set(1, 1, v);
        Assert.Equal(before + 1, s.Palette.Colors.Count);
        Assert.Equal((uint)before, v);
        Assert.Equal(c, s.Palette.Colors[(int)cel.Image.Get(1, 1)]);
        Assert.Equal(v, s.ColorToPixel(c)); // second time: no new entry
        Assert.Equal(before + 1, s.Palette.Colors.Count);
    }

    [Fact]
    public void Indexed_FullPalette_PicksNearestInsteadOfAdding()
    {
        var s = new Sprite(2, 2, ColorMode.Indexed);
        while (s.Palette.Colors.Count < Palette.MaxColors)
            s.Palette.Add(new Rgba((byte)s.Palette.Colors.Count, 0, 0, 255));
        uint v = s.ColorToPixel(new Rgba(100, 3, 0, 255));
        Assert.Equal(Palette.MaxColors, s.Palette.Colors.Count);
        Assert.Equal(new Rgba(100, 0, 0, 255), s.Palette.Colors[(int)v]);
    }

    [Fact]
    public void Indexed_RemovePaletteEntryInUse_RemapsToNearest()
    {
        var s = new Sprite(3, 1, ColorMode.Indexed);
        int red = s.Palette.Add(new Rgba(255, 0, 0, 255));
        int nearRed = s.Palette.Add(new Rgba(250, 10, 0, 255));
        int blue = s.Palette.Add(new Rgba(0, 0, 255, 255));
        var img = s.GetOrCreateCel(s.Layers[0], 0).Image;
        img.Set(0, 0, (uint)red);
        img.Set(1, 0, (uint)nearRed);
        img.Set(2, 0, (uint)blue);

        s.RemovePaletteEntry(red);

        Assert.DoesNotContain(new Rgba(255, 0, 0, 255), s.Palette.Colors);
        Assert.Equal(new Rgba(250, 10, 0, 255), s.Palette.Colors[(int)img.Get(0, 0)]);
        Assert.Equal(new Rgba(250, 10, 0, 255), s.Palette.Colors[(int)img.Get(1, 0)]);
        Assert.Equal(new Rgba(0, 0, 255, 255), s.Palette.Colors[(int)img.Get(2, 0)]);
    }

    [Fact]
    public void ConvertMode_RgbaToIndexedToRgba_PreservesColors()
    {
        var s = new Sprite(4, 2);
        var img = s.GetOrCreateCel(s.Layers[0], 0).Image;
        var colors = new[]
        {
            new Rgba(255, 0, 0, 255), new Rgba(0, 255, 0, 255), new Rgba(0, 0, 255, 128), Rgba.Transparent,
        };
        for (int i = 0; i < 8; i++) img.Pixels[i] = colors[i % 4].Packed;
        var expected = (uint[])img.Pixels.Clone();

        s.ConvertMode(ColorMode.Indexed);
        Assert.Equal(ColorMode.Indexed, s.Mode);
        Assert.Equal(0, s.Palette.TransparentIndex);
        var idx = s.Cels[(s.Layers[0], 0)].Image;
        Assert.Equal(0u, idx.Pixels[3]); // transparent pixel -> transparent index
        Assert.True(s.Palette.Colors.Count <= Palette.MaxColors);

        s.ConvertMode(ColorMode.Rgba);
        Assert.Equal(ColorMode.Rgba, s.Mode);
        Assert.Equal(expected, s.Cels[(s.Layers[0], 0)].Image.Pixels);
    }

    [Fact]
    public void ConvertMode_MoreThan256Colors_StaysWithinPalette()
    {
        var s = new Sprite(32, 32);
        var img = s.GetOrCreateCel(s.Layers[0], 0).Image;
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = new Rgba((byte)(i / 4), (byte)(i % 4 * 60), 7, 255).Packed;
        s.ConvertMode(ColorMode.Indexed);
        Assert.True(s.Palette.Colors.Count <= Palette.MaxColors);
        Assert.All(s.Cels[(s.Layers[0], 0)].Image.Pixels, p => Assert.True(p < (uint)s.Palette.Colors.Count));
    }

    [Fact]
    public void GetOrCreateCel_ReturnsSameCel()
    {
        var s = new Sprite(8, 8);
        var a = s.GetOrCreateCel(s.Layers[0], 0);
        var b = s.GetOrCreateCel(s.Layers[0], 0);
        Assert.Same(a, b);
        Assert.Equal(8, a.Image.Width);
        Assert.Equal(1f, a.Opacity);
    }

    [Fact]
    public void Resize_ShiftsCelsByAnchor()
    {
        var s = new Sprite(10, 10);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        s.Resize(20, 30, Anchor.Center);
        Assert.Equal((20, 30), (s.Width, s.Height));
        Assert.Equal((5, 10), (cel.X, cel.Y));
        s.Resize(10, 10, Anchor.BottomRight);
        Assert.Equal((-5, -10), (cel.X, cel.Y));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Resize(0, 5, Anchor.TopLeft));
    }

    [Fact]
    public void PixelRect_UnionAndIntersect()
    {
        var a = new PixelRect(0, 0, 10, 10);
        var b = new PixelRect(5, 5, 10, 10);
        Assert.Equal(new PixelRect(5, 5, 5, 5), a.Intersect(b));
        Assert.Equal(new PixelRect(0, 0, 15, 15), a.Union(b));
        Assert.True(a.Intersect(new PixelRect(20, 20, 2, 2)).IsEmpty);
    }

    [Fact]
    public void Rgba_PackedIsAabbggrr() =>
        Assert.Equal(0x80030201u, new Rgba(1, 2, 3, 0x80).Packed);
}
