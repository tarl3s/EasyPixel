using System.IO.Compression;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public class FormatTestsEpx
{
    private static Sprite Build(ColorMode mode)
    {
        var sp = new Sprite(7, 5, mode);
        var bg = sp.Layers[0];
        bg.Name = "Background";
        var grp = new Layer { Name = "Group", IsGroup = true, Opacity = 128, Blend = BlendMode.Multiply, Visible = false };
        var child = new Layer { Name = "Child", Parent = grp, Locked = true, Opacity = 200, Blend = BlendMode.Screen };
        sp.Layers.Add(grp);
        sp.Layers.Add(child);
        sp.Frames[0].DurationMs = 40;
        sp.Frames.Add(new Frame { DurationMs = 250 });
        sp.Tags.Add(new Tag { Name = "walk", From = 0, To = 1, Dir = LoopDir.PingPong, Color = new Rgba(1, 2, 3, 4) });
        if (mode == ColorMode.Indexed)
        {
            sp.Palette.Colors.Add(new Rgba(255, 0, 0, 255));
            sp.Palette.Colors.Add(new Rgba(0, 255, 0, 128));
        }
        var rnd = new Random(5);
        foreach (var (l, f) in new[] { (bg, 0), (bg, 1), (child, 1) })
        {
            var cel = sp.GetOrCreateCel(l, f);
            cel.X = f - 2; cel.Y = 3; cel.Opacity = 0.5f;
            for (int i = 0; i < cel.Image.Pixels.Length; i++)
                cel.Image.Pixels[i] = mode == ColorMode.Indexed ? (uint)rnd.Next(3) : (uint)rnd.NextInt64(0, uint.MaxValue);
        }
        sp.UnknownChunks["abcd"] = new byte[] { 1, 2, 3, 255 };
        bg.Extra["ase:after"] = new byte[] { 9, 8 };
        sp.Frames[1].Extra["ase:chunks"] = new byte[] { 7 };
        sp.Tags[0].Extra["ase:repeat"] = new byte[] { 2, 0 };
        sp.Cels[(child, 1)].Extra["ase:z"] = new byte[] { 1, 0 };
        return sp;
    }

    [Fact]
    public void Png_RoundTrip_Random64()
    {
        var img = new Image(64, 64);
        var rnd = new Random(1);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (uint)rnd.NextInt64(0, uint.MaxValue);
        var back = Png.Decode(Png.Encode(img));
        Assert.Equal(64, back.Width);
        Assert.Equal(img.Pixels, back.Pixels);
    }

    [Fact]
    public void Png_Grey8_RoundTrip()
    {
        var img = new Image(5, 3);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (uint)(i * 17);
        Assert.Equal(img.Pixels, Png.DecodeGrey8(Png.EncodeGrey8(img)).Pixels);
    }

    [Fact]
    public void Png_BadCrc_And_Truncated_ThrowFormatException()
    {
        var bytes = Png.Encode(new Image(4, 4, 0xFF112233));
        var bad = (byte[])bytes.Clone();
        bad[^20] ^= 0xFF;
        Assert.Throws<FormatException>(() => Png.Decode(bad));
        for (int n = 0; n < bytes.Length; n += 7)
            Assert.Throws<FormatException>(() => Png.Decode(bytes[..n]));
    }

    [Theory]
    [InlineData(ColorMode.Rgba)]
    [InlineData(ColorMode.Indexed)]
    public void Epx_RoundTrip_AllModelFields(ColorMode mode)
    {
        var sp = Build(mode);
        var ms = new MemoryStream();
        var warnings = new List<string>();
        new EpxFormat().Write(sp, ms, warnings);
        Assert.Empty(warnings);
        ms.Position = 0;
        var r = new EpxFormat().Read(ms, warnings);

        Assert.Equal((sp.Width, sp.Height, sp.Mode), (r.Width, r.Height, r.Mode));
        Assert.Equal(sp.Palette.Colors, r.Palette.Colors);
        Assert.Equal(sp.Palette.TransparentIndex, r.Palette.TransparentIndex);
        Assert.Equal(sp.Layers.Count, r.Layers.Count);
        for (int i = 0; i < sp.Layers.Count; i++)
        {
            var a = sp.Layers[i]; var b = r.Layers[i];
            Assert.Equal((a.Name, a.Visible, a.Locked, a.Opacity, a.Blend, a.IsGroup), (b.Name, b.Visible, b.Locked, b.Opacity, b.Blend, b.IsGroup));
            Assert.Equal(a.Parent == null ? -1 : sp.Layers.IndexOf(a.Parent), b.Parent == null ? -1 : r.Layers.IndexOf(b.Parent));
        }
        Assert.Equal(sp.Frames.Select(f => f.DurationMs), r.Frames.Select(f => f.DurationMs));
        Assert.Equal(sp.Tags.Count, r.Tags.Count);
        Assert.Equal((sp.Tags[0].Name, sp.Tags[0].From, sp.Tags[0].To, sp.Tags[0].Dir, sp.Tags[0].Color),
                     (r.Tags[0].Name, r.Tags[0].From, r.Tags[0].To, r.Tags[0].Dir, r.Tags[0].Color));
        Assert.Equal(sp.Cels.Count, r.Cels.Count);
        foreach (var ((layer, frame), cel) in sp.Cels)
        {
            var rc = r.Cels[(r.Layers[sp.Layers.IndexOf(layer)], frame)];
            Assert.Equal((cel.X, cel.Y, cel.Opacity), (rc.X, rc.Y, rc.Opacity));
            Assert.Equal(cel.Image.Pixels, rc.Image.Pixels);
        }
        Assert.Equal(sp.UnknownChunks["abcd"], r.UnknownChunks["abcd"]);
        Assert.Equal(new byte[] { 9, 8 }, r.Layers[0].Extra["ase:after"]);
        Assert.Empty(r.Layers[1].Extra);
        Assert.Equal(new byte[] { 7 }, r.Frames[1].Extra["ase:chunks"]);
        Assert.Empty(r.Frames[0].Extra);
        Assert.Equal(new byte[] { 2, 0 }, r.Tags[0].Extra["ase:repeat"]);
        Assert.Equal(new byte[] { 1, 0 }, r.Cels[(r.Layers[2], 1)].Extra["ase:z"]);
        Assert.Empty(r.Cels[(r.Layers[0], 0)].Extra);
    }

    private static byte[] ZipWithJson(string json)
    {
        var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
        using (var w = new StreamWriter(z.CreateEntry("sprite.json").Open())) w.Write(json);
        return ms.ToArray();
    }

    [Fact]
    public void Epx_NewerFormatVersion_ReadableError()
    {
        var ex = Assert.Throws<FormatException>(() => new EpxFormat().Read(new MemoryStream(ZipWithJson("{\"format\":2}")), new List<string>()));
        Assert.Contains("newer EasyPixel", ex.Message);
    }

    [Fact]
    public void Epx_Truncated_ThrowsFormatException_NotCrash()
    {
        var ms = new MemoryStream();
        new EpxFormat().Write(Build(ColorMode.Rgba), ms, new List<string>());
        var bytes = ms.ToArray();
        for (int n = 0; n < bytes.Length; n += Math.Max(1, bytes.Length / 40))
            Assert.Throws<FormatException>(() => new EpxFormat().Read(new MemoryStream(bytes[..n]), new List<string>()));
        Assert.Throws<FormatException>(() => new EpxFormat().Read(new MemoryStream(ZipWithJson("not json")), new List<string>()));
        Assert.Throws<FormatException>(() => new EpxFormat().Read(new MemoryStream(new byte[] { 1, 2, 3 }), new List<string>()));
    }

    [Fact]
    public void Epx_UnknownKeys_Ignored()
    {
        var json = "{\"format\":1,\"future\":{\"a\":1},\"width\":2,\"height\":2,\"mode\":\"rgba\",\"palette\":{\"transparentIndex\":-1,\"colors\":[]},\"tags\":[],\"layers\":[{\"name\":\"L\",\"visible\":true,\"locked\":false,\"opacity\":255,\"blend\":\"Normal\",\"isGroup\":false,\"parent\":-1}],\"frames\":[{\"durationMs\":10}],\"cels\":[]}";
        var sp = new EpxFormat().Read(new MemoryStream(ZipWithJson(json)), new List<string>());
        Assert.Equal("L", sp.Layers[0].Name);
    }

    [Fact]
    public void Registry_FindsByExtension_AndPngWarnsOnMultiFrame()
    {
        Assert.IsType<EpxFormat>(FormatRegistry.ForPath("a/b/x.EPX"));
        Assert.IsType<PngFormat>(FormatRegistry.ForPath("x.png"));
        Assert.Null(FormatRegistry.ForPath("x.unknown"));
        var sp = Build(ColorMode.Rgba);
        var w = new List<string>();
        var ms = new MemoryStream();
        new PngFormat().Write(sp, ms, w);
        Assert.Contains("Only the first frame was saved", w);
        ms.Position = 0;
        var back = new PngFormat().Read(ms, w);
        Assert.Equal((7, 5), (back.Width, back.Height));
    }
}
