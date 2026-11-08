using EasyPixel.Core.Ai;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public class PixelArtConverterTests
{
    private static readonly uint White = new Rgba(255, 255, 255, 255).Packed;
    private static readonly uint Red = new Rgba(220, 30, 30, 255).Packed;

    private static void Fill(Image img, int x0, int y0, int w, int h, uint c)
    {
        for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) img.Set(x, y, c);
    }

    private static Image Disc()
    {
        var img = new Image(512, 512, White);
        for (int y = 0; y < 512; y++)
            for (int x = 0; x < 512; x++)
                if ((x - 256) * (x - 256) + (y - 256) * (y - 256) <= 150 * 150) img.Set(x, y, Red);
        return img;
    }

    [Fact]
    public void Convert_RedCircleOnWhite_GivesTransparentBgAndOutline()
    {
        var r = PixelArtConverter.Convert(Disc(), new ConvertOptions(32))!;
        Assert.Equal(32, r.Width);
        Assert.Equal(0u, r.Get(0, 0));
        Assert.Equal(0u, r.Get(31, 0));
        Assert.Equal(0u, r.Get(0, 31));
        Assert.Equal(0u, r.Get(31, 31));
        var mid = Rgba.FromPacked(r.Get(16, 16));
        Assert.True(mid.R > 150 && mid.G < 100);
        Assert.True(r.Pixels.Distinct().Count() <= 13); // 12 colours + transparent
        Assert.All(r.Pixels, p => Assert.True((p >> 24) == 0 || (p >> 24) == 255));
        // Walking in from the left edge, the first opaque pixel is the dark outline.
        int x = 0;
        while (r.Get(x, 16) == 0) x++;
        Assert.True(Rgba.FromPacked(r.Get(x, 16)).R < 140);
    }

    [Fact]
    public void Downscale_KeepsSmallFeature()
    {
        var img = new Image(512, 512, White);
        Fill(img, 56, 56, 400, 400, new Rgba(150, 150, 150, 255).Packed);
        Fill(img, 200, 200, 16, 16, new Rgba(0, 0, 0, 255).Packed);
        var r = PixelArtConverter.Convert(img, new ConvertOptions(32, Outline: false))!;
        Assert.Contains(r.Pixels, p => (p >> 24) == 255 && Rgba.FromPacked(p).R < 60);
    }

    [Fact]
    public void Convert_UsesGivenPalette()
    {
        var pal = new List<Rgba> { new(200, 0, 0, 255), new(0, 0, 200, 255) };
        var r = PixelArtConverter.Convert(Disc(), new ConvertOptions(32, Palette: pal))!;
        var allowed = pal.Select(c => c.Packed)
            .Concat(pal.Select(c => new Rgba((byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2), 255).Packed)).ToHashSet();
        Assert.All(r.Pixels.Where(p => p != 0), p => Assert.Contains(p, allowed));
    }

    [Fact]
    public void Convert_EmptyPicture_ReturnsNull()
    {
        Assert.Null(PixelArtConverter.Convert(new Image(128, 128, White), new ConvertOptions(32)));
        Assert.Null(PixelArtConverter.Convert(new Image(128, 128), new ConvertOptions(32)));
    }

    [Fact]
    public void Convert_NoisyBackground_StillIsolatesSubject()
    {
        var rnd = new Random(7);
        var img = new Image(256, 256);
        byte V() => (byte)(235 + rnd.Next(-20, 21));
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = new Rgba(V(), V(), V(), 255).Packed;
        Fill(img, 80, 80, 100, 100, new Rgba(20, 40, 220, 255).Packed);
        var r = PixelArtConverter.Convert(img, new ConvertOptions(32))!;
        Assert.Equal(0u, r.Get(0, 0));
        Assert.Equal(0u, r.Get(31, 0));
        Assert.Equal(0u, r.Get(0, 31));
        Assert.Equal(0u, r.Get(31, 31));
        Assert.Contains(r.Pixels, p => (p >> 24) != 0 && Rgba.FromPacked(p).B > 150);
    }

    [Fact]
    public void Convert_SubjectFeetAtBottom()
    {
        var r = PixelArtConverter.Convert(Disc(), new ConvertOptions(32, Outline: false))!;
        int last = -1;
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (r.Get(x, y) != 0) last = y;
        Assert.Equal(30, last);
    }
}
