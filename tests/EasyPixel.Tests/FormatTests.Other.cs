using System.Text;
using System.Text.Json;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Tests;

public class FormatTestsOther
{
    private static Sprite Anim(int w, int h, int frames, params int[] durations)
    {
        var sp = new Sprite(w, h);
        sp.Layers[0].Name = "Base";
        var top = new Layer { Name = "Top", Opacity = 128 };
        sp.Layers.Add(top);
        for (int f = 1; f < frames; f++) sp.Frames.Add(new Frame());
        for (int f = 0; f < frames; f++)
        {
            sp.Frames[f].DurationMs = durations[f % durations.Length];
            foreach (var l in sp.Layers)
            {
                var img = sp.GetOrCreateCel(l, f).Image;
                for (int i = 0; i < img.Pixels.Length; i++)
                    img.Pixels[i] = (i + f + l.Name.Length) % 5 == 0 ? 0u : new Rgba((byte)(40 * f + 10), (byte)(i * 7), (byte)(l.Name.Length * 20), 255).Packed;
            }
        }
        return sp;
    }

    private static Sprite Anim1(int w, int h, int frames, params int[] durations)
    {
        var sp = Anim(w, h, frames, durations);
        var top = sp.Layers[1];
        sp.Layers.Remove(top);
        foreach (var k in sp.Cels.Keys.Where(k => k.Item1 == top).ToList()) sp.Cels.Remove(k);
        return sp;
    }

    private static Sprite RoundTrip(ISpriteFormat fmt, Sprite sp, List<string> warnings)
    {
        var ms = new MemoryStream();
        fmt.Write(sp, ms, warnings);
        ms.Position = 0;
        return fmt.Read(ms, new List<string>());
    }

    private static Image Flat(Sprite sp, int f)
    {
        var img = new Image(sp.Width, sp.Height);
        Compositor.Flatten(sp, f, img, new PixelRect(0, 0, sp.Width, sp.Height));
        return img;
    }

    // ---- piskel ----

    [Fact]
    public void Piskel_RoundTrip()
    {
        var sp = Anim(5, 4, 3, 100);
        var warnings = new List<string>();
        var back = RoundTrip(new PiskelFormat(), sp, warnings);
        Assert.Empty(warnings);
        Assert.Equal((5, 4, 3, 2), (back.Width, back.Height, back.Frames.Count, back.Layers.Count));
        for (int l = 0; l < 2; l++)
        {
            Assert.Equal(sp.Layers[l].Name, back.Layers[l].Name);
            Assert.Equal(sp.Layers[l].Opacity, back.Layers[l].Opacity);
            for (int f = 0; f < 3; f++)
                Assert.Equal(sp.Cels[(sp.Layers[l], f)].Image.Pixels, back.Cels[(back.Layers[l], f)].Image.Pixels);
        }
        Assert.All(back.Frames, fr => Assert.Equal(100, fr.DurationMs));
    }

    [Fact]
    public void Piskel_WritesModelVersion2_WithStripLayout()
    {
        var ms = new MemoryStream();
        new PiskelFormat().Write(Anim(3, 2, 3, 100), ms, new List<string>());
        using var doc = JsonDocument.Parse(ms.ToArray());
        Assert.Equal(2, doc.RootElement.GetProperty("modelVersion").GetInt32());
        var p = doc.RootElement.GetProperty("piskel");
        Assert.Equal(10, p.GetProperty("fps").GetInt32());
        using var layer = JsonDocument.Parse(p.GetProperty("layers")[0].GetString()!);
        Assert.Equal(3, layer.RootElement.GetProperty("frameCount").GetInt32());
        Assert.Equal("[[0],[1],[2]]", layer.RootElement.GetProperty("chunks")[0].GetProperty("layout").GetRawText());
    }

    [Fact]
    public void Piskel_ModelVersion1_Readable()
    {
        var strip = new Image(4, 2);
        for (int i = 0; i < strip.Pixels.Length; i++) strip.Pixels[i] = new Rgba((byte)(i * 30), 1, 2, 255).Packed;
        string layer = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["name"] = "Old", ["opacity"] = 1, ["frameCount"] = 2,
            ["base64PNG"] = "data:image/png;base64," + Convert.ToBase64String(Png.Encode(strip)),
        });
        string json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["modelVersion"] = 1,
            ["piskel"] = new Dictionary<string, object>
            {
                ["name"] = "x", ["description"] = "", ["fps"] = 4, ["width"] = 2, ["height"] = 2,
                ["layers"] = new[] { layer },
            },
        });
        var sp = new PiskelFormat().Read(new MemoryStream(Encoding.UTF8.GetBytes(json)), new List<string>());
        Assert.Equal((2, 2, 2), (sp.Width, sp.Height, sp.Frames.Count));
        Assert.All(sp.Frames, f => Assert.Equal(250, f.DurationMs));
        Assert.Equal("Old", sp.Layers[0].Name);
        Assert.Equal(strip.Get(2, 1), sp.Cels[(sp.Layers[0], 1)].Image.Get(0, 1));
    }

    [Fact]
    public void Piskel_Warnings_ForDurationsHiddenGroupsAndBlend()
    {
        var sp = Anim(3, 3, 2, 100, 300);
        sp.Layers[0].Visible = false;
        sp.Layers[1].Blend = BlendMode.Multiply;
        sp.Layers.Add(new Layer { Name = "G", IsGroup = true });
        var w = new List<string>();
        var back = RoundTrip(new PiskelFormat(), sp, w);
        Assert.Contains("Frame durations were averaged (Piskel stores one speed)", w);
        Assert.Contains("Hidden layers were not saved", w);
        Assert.Contains("Layer groups were flattened", w);
        Assert.Contains(w, x => x.Contains("blend", StringComparison.OrdinalIgnoreCase));
        Assert.Single(back.Layers);
        Assert.All(back.Frames, f => Assert.Equal(200, f.DurationMs));
    }

    [Fact]
    public void Piskel_Garbage_IsFormatException()
    {
        Assert.Throws<FormatException>(() => new PiskelFormat().Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"a\":1}")), new()));
        Assert.Throws<FormatException>(() => new PiskelFormat().Read(new MemoryStream(Encoding.UTF8.GetBytes("nope")), new()));
    }

    // ---- gif ----

    [Fact]
    public void Gif_WriteRead_FramesAndDelays()
    {
        var sp = Anim1(8, 6, 3, 40, 250, 100);
        var w = new List<string>();
        var back = RoundTrip(new GifFormat(), sp, w);
        Assert.Empty(w);
        Assert.Equal((8, 6, 3, 1), (back.Width, back.Height, back.Frames.Count, back.Layers.Count));
        Assert.Equal(new[] { 40, 250, 100 }, back.Frames.Select(f => f.DurationMs).ToArray());
        for (int f = 0; f < 3; f++)
        {
            var want = Flat(sp, f);
            var got = Flat(back, f);
            for (int i = 0; i < want.Pixels.Length; i++)
            {
                if ((want.Pixels[i] >> 24) == 0) Assert.Equal(0, (int)(got.Pixels[i] >> 24));
                else Assert.Equal(want.Pixels[i], got.Pixels[i]);
            }
        }
        Assert.Contains(Enumerable.Range(0, 3), f => Flat(back, f).Pixels.Any(p => (p >> 24) == 0));
    }

    [Fact]
    public void Gif_Write_WarnsAboutSemiTransparency_AndWritesNetscapeLoop()
    {
        var sp = new Sprite(2, 1);
        var img = sp.GetOrCreateCel(sp.Layers[0], 0).Image;
        img.Pixels[0] = new Rgba(10, 20, 30, 100).Packed;
        img.Pixels[1] = new Rgba(10, 20, 30, 200).Packed;
        var w = new List<string>();
        var ms = new MemoryStream();
        new GifFormat().Write(sp, ms, w);
        Assert.Contains("Semi-transparent pixels were made solid", w);
        var bytes = ms.ToArray();
        Assert.Equal("GIF89a", Encoding.ASCII.GetString(bytes, 0, 6));
        Assert.Contains("NETSCAPE2.0", Encoding.ASCII.GetString(bytes));
        var back = new GifFormat().Read(new MemoryStream(bytes), new());
        var px = Flat(back, 0).Pixels;
        Assert.Equal(0, (int)(px[0] >> 24));
        Assert.Equal(255, (int)(px[1] >> 24));
    }

    [Fact]
    public void Gif_ManyColours_StayWithinPalette()
    {
        var sp = new Sprite(32, 32);
        var img = sp.GetOrCreateCel(sp.Layers[0], 0).Image;
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = new Rgba((byte)(i % 32 * 8), (byte)(i / 32 * 8), (byte)(i * 3), 255).Packed;
        var back = RoundTrip(new GifFormat(), sp, new List<string>());
        Assert.All(Flat(back, 0).Pixels, p => Assert.Equal(255, (int)(p >> 24)));
        Assert.True(Flat(back, 0).Pixels.Distinct().Count() <= 255);
    }

    [Fact]
    public void Gif_LargeNoisyImage_FillsLzwTable_AndRoundTrips()
    {
        var sp = new Sprite(120, 90);
        var img = sp.GetOrCreateCel(sp.Layers[0], 0).Image;
        var rnd = new Random(8);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = new Rgba((byte)(rnd.Next(6) * 50), (byte)(rnd.Next(6) * 50), (byte)(rnd.Next(6) * 50), 255).Packed;
        var back = RoundTrip(new GifFormat(), sp, new List<string>());
        Assert.Equal(img.Pixels, Flat(back, 0).Pixels);
    }

    // Minimal GIF writer for hand-made files: 4-colour palette, LZW as clear + two literals per group (the code size never grows).
    private static byte[] HandGif(int w, int h, params (int x, int y, int fw, int fh, int disposal, byte[] idx)[] frames)
    {
        var o = new MemoryStream();
        void B(params int[] v) { foreach (var b in v) o.WriteByte((byte)b); }
        void U16(int v) { B(v & 255, v >> 8); }
        o.Write(Encoding.ASCII.GetBytes("GIF89a"));
        U16(w); U16(h); B(0x80 | 1, 0, 0); // global table, 4 colours
        B(0, 0, 0, 255, 0, 0, 0, 0, 255, 0, 255, 0); // 0 black, 1 red, 2 blue, 3 green
        foreach (var f in frames)
        {
            B(0x21, 0xF9, 4, f.disposal << 2, 10, 0, 255, 0); // 100 ms, no transparency
            B(0x2C); U16(f.x); U16(f.y); U16(f.fw); U16(f.fh); B(0);
            B(2); // min code size
            var bits = new List<bool>();
            void Code(int c) { for (int i = 0; i < 3; i++) bits.Add(((c >> i) & 1) != 0); }
            for (int i = 0; i < f.idx.Length; i += 2)
            {
                Code(4);
                Code(f.idx[i]);
                if (i + 1 < f.idx.Length) Code(f.idx[i + 1]);
            }
            Code(4); Code(5);
            var data = new List<byte>();
            for (int i = 0; i < bits.Count; i += 8)
            {
                int v = 0;
                for (int k = 0; k < 8 && i + k < bits.Count; k++) if (bits[i + k]) v |= 1 << k;
                data.Add((byte)v);
            }
            for (int i = 0; i < data.Count; i += 255)
            {
                int n = Math.Min(255, data.Count - i);
                B(n);
                o.Write(data.GetRange(i, n).ToArray());
            }
            B(0);
        }
        B(0x3B);
        return o.ToArray();
    }

    private static byte[] Fill(int n, byte v) => Enumerable.Repeat(v, n).ToArray();
    private const uint Red = 0xFF0000FF, Blue = 0xFFFF0000, Green = 0xFF00FF00;

    [Fact]
    public void Gif_Disposal_RestoreBackground()
    {
        var bytes = HandGif(4, 4,
            (0, 0, 4, 4, 1, Fill(16, 1)),
            (1, 1, 2, 2, 2, Fill(4, 2)),
            (0, 0, 1, 1, 0, Fill(1, 3)));
        var sp = new GifFormat().Read(new MemoryStream(bytes), new());
        Assert.Equal(3, sp.Frames.Count);
        var f0 = Flat(sp, 0); var f1 = Flat(sp, 1); var f2 = Flat(sp, 2);
        Assert.All(f0.Pixels, p => Assert.Equal(Red, p));
        Assert.Equal(Blue, f1.Get(1, 1));
        Assert.Equal(Blue, f1.Get(2, 2));
        Assert.Equal(Red, f1.Get(0, 0));
        Assert.Equal(0, (int)(f2.Get(1, 1) >> 24)); // disposed to transparent
        Assert.Equal(0, (int)(f2.Get(2, 2) >> 24));
        Assert.Equal(Green, f2.Get(0, 0));
        Assert.Equal(Red, f2.Get(3, 3));
        Assert.All(sp.Frames, f => Assert.Equal(100, f.DurationMs));
    }

    [Fact]
    public void Gif_Disposal_RestorePrevious()
    {
        var bytes = HandGif(4, 4,
            (0, 0, 4, 4, 1, Fill(16, 1)),
            (1, 1, 2, 2, 3, Fill(4, 2)),
            (0, 0, 1, 1, 0, Fill(1, 3)));
        var sp = new GifFormat().Read(new MemoryStream(bytes), new());
        Assert.Equal(Blue, Flat(sp, 1).Get(1, 1));
        Assert.Equal(Red, Flat(sp, 2).Get(1, 1));
        Assert.Equal(Green, Flat(sp, 2).Get(0, 0));
    }

    [Fact]
    public void Gif_Truncated_ReadableError()
    {
        var ms = new MemoryStream();
        new GifFormat().Write(Anim1(8, 6, 3, 100), ms, new List<string>());
        var full = ms.ToArray();
        foreach (int cut in new[] { 3, 10, 20, full.Length / 2, full.Length - 1 })
        {
            var ex = Record.Exception(() => new GifFormat().Read(new MemoryStream(full[..cut]), new()));
            Assert.IsType<FormatException>(ex);
        }
        Assert.Throws<FormatException>(() => new GifFormat().Read(new MemoryStream(Encoding.ASCII.GetBytes("NOTAGIF89a....")), new()));
    }

    [Fact]
    public void Gif_CorruptLzw_NeverHangs()
    {
        var ms = new MemoryStream();
        new GifFormat().Write(Anim1(8, 6, 2, 100), ms, new List<string>());
        var bytes = ms.ToArray();
        var rnd = new Random(3);
        for (int round = 0; round < 50; round++)
        {
            var bad = (byte[])bytes.Clone();
            for (int k = 0; k < 6; k++) bad[rnd.Next(13, bad.Length)] = (byte)rnd.Next(256);
            var ex = Record.Exception(() => new GifFormat().Read(new MemoryStream(bad), new()));
            Assert.True(ex == null || ex is FormatException, ex?.ToString());
        }
    }

    [Fact]
    public void Registry_KnowsPiskelAndGif()
    {
        Assert.IsType<PiskelFormat>(FormatRegistry.ForPath("a.piskel"));
        Assert.IsType<GifFormat>(FormatRegistry.ForPath("a.GIF"));
    }

    // ---- sheets ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    [InlineData(3, 1)]
    public void Sheet_ExportSlice_RoundTrip(int kind, int padding)
    {
        var sp = Anim(4, 3, 5, 100, 80);
        sp.Tags.Add(new Tag { Name = "run", From = 1, To = 3, Dir = LoopDir.PingPong });
        var layout = kind switch { 0 => SheetLayout.Horizontal, 1 => SheetLayout.Vertical, 2 => SheetLayout.Grid(2), _ => SheetLayout.Grid(3) };
        var (sheet, json) = SpriteSheet.Export(sp, layout, padding);
        var back = SpriteSheet.Slice(sheet, 4, 3, padding);
        Assert.Equal(5, back.Frames.Count);
        for (int f = 0; f < 5; f++)
            Assert.Equal(Flat(sp, f).Pixels, Flat(back, f).Pixels);

        using var doc = JsonDocument.Parse(json);
        var frames = doc.RootElement.GetProperty("frames");
        Assert.Equal(5, frames.GetArrayLength());
        Assert.Equal(80, frames[1].GetProperty("durationMs").GetInt32());
        Assert.Equal(4, frames[1].GetProperty("w").GetInt32());
        Assert.Equal(3, frames[1].GetProperty("h").GetInt32());
        var t = doc.RootElement.GetProperty("tags")[0];
        Assert.Equal(("run", 1, 3), (t.GetProperty("name").GetString(), t.GetProperty("from").GetInt32(), t.GetProperty("to").GetInt32()));
        Assert.Equal("pingpong", t.GetProperty("direction").GetString());
        Assert.Equal(sheet.Width, doc.RootElement.GetProperty("size").GetProperty("w").GetInt32());
        Assert.Equal(sheet.Height, doc.RootElement.GetProperty("size").GetProperty("h").GetInt32());
        for (int f = 0; f < 5; f++)
        {
            int x = frames[f].GetProperty("x").GetInt32(), y = frames[f].GetProperty("y").GetInt32();
            var flat = Flat(sp, f);
            for (int j = 0; j < 3; j++) for (int i = 0; i < 4; i++) Assert.Equal(flat.Get(i, j), sheet.Get(x + i, y + j));
        }
    }

    [Fact]
    public void Sheet_Dimensions()
    {
        var sp = Anim(4, 3, 5, 100);
        Assert.Equal((4 * 5 + 2 * 4, 3), Dim(SpriteSheet.Export(sp, SheetLayout.Horizontal, 2).sheet));
        Assert.Equal((4, 3 * 5 + 2 * 4), Dim(SpriteSheet.Export(sp, SheetLayout.Vertical, 2).sheet));
        Assert.Equal((4 * 2 + 1, 3 * 3 + 2), Dim(SpriteSheet.Export(sp, SheetLayout.Grid(2), 1).sheet));
    }

    private static (int, int) Dim(Image i) => (i.Width, i.Height);

    [Fact]
    public void Sheet_Slice_DropsEmptyTrailingCells_AndPlainPngOpensAsOneFrame()
    {
        var img = new Image(8, 4);
        img.Set(1, 1, 0xFF112233);
        var sp = SpriteSheet.Slice(img, 4, 4, 0);
        Assert.Single(sp.Frames);
        var png = new PngFormat().Read(new MemoryStream(Png.Encode(img)), new());
        Assert.Single(png.Frames);
        Assert.Throws<ArgumentException>(() => SpriteSheet.Slice(img, 20, 4, 0));
    }
}
