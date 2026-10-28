using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

/// <summary>
/// Builds and parses .aseprite bytes by hand, straight from the public spec, without the production code,
/// so a bug shared by the writer and the reader cannot hide.
/// </summary>
internal static class AseHand
{
    public sealed class Buf
    {
        private readonly MemoryStream _ms = new();
        public Buf U8(int v) { _ms.WriteByte((byte)v); return this; }
        public Buf U16(int v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)v); _ms.Write(b); return this; }
        public Buf S16(int v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteInt16LittleEndian(b, (short)v); _ms.Write(b); return this; }
        public Buf U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); _ms.Write(b); return this; }
        public Buf Zero(int n) { for (int i = 0; i < n; i++) _ms.WriteByte(0); return this; }
        public Buf Bytes(byte[] b) { _ms.Write(b); return this; }
        public Buf Str(string s) { var b = Encoding.UTF8.GetBytes(s); U16(b.Length); return Bytes(b); }
        public byte[] ToArray() => _ms.ToArray();
    }

    public static byte[] Chunk(int type, Action<Buf> body)
    {
        var data = new Buf();
        body(data);
        var d = data.ToArray();
        return new Buf().U32((uint)(6 + d.Length)).U16(type).Bytes(d).ToArray();
    }

    public static byte[] Frame(int durationMs, params byte[][] chunks)
    {
        var body = chunks.SelectMany(c => c).ToArray();
        return new Buf().U32((uint)(16 + body.Length)).U16(0xF1FA).U16(chunks.Length).U16(durationMs).Zero(2)
            .U32((uint)chunks.Length).Bytes(body).ToArray();
    }

    public static byte[] File(int w, int h, int depth, uint flags, int transparent, int ncolors, params byte[][] frames)
    {
        var body = frames.SelectMany(f => f).ToArray();
        return new Buf().U32((uint)(128 + body.Length)).U16(0xA5E0).U16(frames.Length).U16(w).U16(h).U16(depth)
            .U32(flags).U16(100).Zero(8).U8(transparent).Zero(3).U16(ncolors)
            .U8(1).U8(1).S16(0).S16(0).U16(16).U16(16).Zero(84).Bytes(body).ToArray();
    }

    public static byte[] Layer(string name, int flags = 3, int type = 0, int level = 0, int blend = 0, int opacity = 255) =>
        Chunk(0x2004, b => b.U16(flags).U16(type).U16(level).U16(0).U16(0).U16(blend).U8(opacity).Zero(3).Str(name));

    public static byte[] RawCel(int layer, int x, int y, int opacity, int w, int h, byte[] pixels) =>
        Chunk(0x2005, b => b.U16(layer).S16(x).S16(y).U8(opacity).U16(0).S16(0).Zero(5).U16(w).U16(h).Bytes(pixels));

    public static byte[] ZCel(int layer, int x, int y, int opacity, int w, int h, byte[] pixels)
    {
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Fastest, true)) zs.Write(pixels);
        return Chunk(0x2005, b => b.U16(layer).S16(x).S16(y).U8(opacity).U16(2).S16(0).Zero(5).U16(w).U16(h).Bytes(z.ToArray()));
    }

    public static byte[] LinkCel(int layer, int frame) =>
        Chunk(0x2005, b => b.U16(layer).S16(0).S16(0).U8(255).U16(1).S16(0).Zero(5).U16(frame));

    public static byte[] Palette(params Rgba[] colors) => Chunk(0x2019, b =>
    {
        b.U32((uint)colors.Length).U32(0).U32((uint)colors.Length - 1).Zero(8);
        foreach (var c in colors) b.U16(0).U8(c.R).U8(c.G).U8(c.B).U8(c.A);
    });

    public static byte[] Tags(params (int from, int to, int dir, Rgba color, string name)[] tags) => Chunk(0x2018, b =>
    {
        b.U16(tags.Length).Zero(8);
        foreach (var t in tags) b.U16(t.from).U16(t.to).U8(t.dir).U16(0).Zero(6).U8(t.color.R).U8(t.color.G).U8(t.color.B).U8(0).Str(t.name);
    });

    public static byte[] UserData(string? text, Rgba? color) => Chunk(0x2020, b =>
    {
        b.U32((text != null ? 1u : 0) | (color != null ? 2u : 0));
        if (text != null) b.Str(text);
        if (color is { } c) b.U8(c.R).U8(c.G).U8(c.B).U8(c.A);
    });

    public static int U16At(byte[] b, int at) => BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(at));

    /// <summary>Frames → chunks (type, data without the 6-byte chunk header). Asserts every size field.</summary>
    public static List<List<(int Type, byte[] Data)>> Parse(byte[] file)
    {
        Assert.Equal((uint)file.Length, BinaryPrimitives.ReadUInt32LittleEndian(file));
        Assert.Equal(0xA5E0, U16At(file, 4));
        int frames = U16At(file, 6), pos = 128;
        var result = new List<List<(int, byte[])>>();
        for (int f = 0; f < frames; f++)
        {
            int size = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(pos));
            Assert.Equal(0xF1FA, U16At(file, pos + 4));
            int n = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(pos + 12));
            Assert.Equal(Math.Min(n, 0xFFFF), U16At(file, pos + 6));
            int end = pos + size, p = pos + 16;
            var chunks = new List<(int, byte[])>();
            for (int k = 0; k < n; k++)
            {
                int cs = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(p));
                chunks.Add((U16At(file, p + 4), file[(p + 6)..(p + cs)]));
                p += cs;
            }
            Assert.Equal(end, p);
            result.Add(chunks);
            pos = end;
        }
        Assert.Equal(file.Length, pos);
        return result;
    }
}

public class FormatTestsAseprite
{
    private static readonly AsepriteFormat Ase = new();

    private static Sprite Build(ColorMode mode)
    {
        var sp = new Sprite(7, 5, mode);
        var bg = sp.Layers[0];
        bg.Name = "Background";
        var grp = new Layer { Name = "Group", IsGroup = true, Opacity = 128, Blend = BlendMode.Multiply, Visible = false };
        var child = new Layer { Name = "Child", Parent = grp, Locked = true, Opacity = 200, Blend = BlendMode.Screen };
        var top = new Layer { Name = "Top ✨", Blend = BlendMode.Divide };
        sp.Layers.Add(grp);
        sp.Layers.Add(child);
        sp.Layers.Add(top);
        sp.Frames[0].DurationMs = 40;
        sp.Frames.Add(new Frame { DurationMs = 250 });
        sp.Frames.Add(new Frame { DurationMs = 1000 });
        sp.Tags.Add(new Tag { Name = "walk", From = 0, To = 1, Dir = LoopDir.PingPong, Color = new Rgba(1, 2, 3, 4) });
        sp.Tags.Add(new Tag { Name = "idle", From = 2, To = 2, Dir = LoopDir.Reverse, Color = new Rgba(9, 8, 7, 255) });
        if (mode == ColorMode.Indexed)
        {
            sp.Palette.Colors.Add(new Rgba(255, 0, 0, 255));
            sp.Palette.Colors.Add(new Rgba(0, 255, 0, 128));
        }
        var rnd = new Random(5);
        foreach (var (l, f) in new[] { (bg, 0), (bg, 1), (child, 1), (top, 0) })
        {
            var cel = sp.GetOrCreateCel(l, f);
            cel.X = f - 2; cel.Y = 3; cel.Opacity = 128 / 255f;
            for (int i = 0; i < cel.Image.Pixels.Length; i++)
                cel.Image.Pixels[i] = mode == ColorMode.Indexed ? (uint)rnd.Next(3) : (uint)rnd.NextInt64(0, uint.MaxValue);
        }
        sp.Cels[(top, 2)] = sp.Cels[(top, 0)]; // linked cel
        return sp;
    }

    private static byte[] Save(Sprite sp, List<string>? warnings = null)
    {
        var ms = new MemoryStream();
        Ase.Write(sp, ms, warnings ?? new List<string>());
        return ms.ToArray();
    }

    private static Sprite Load(byte[] bytes, List<string>? warnings = null) => Ase.Read(new MemoryStream(bytes), warnings ?? new List<string>());

    [Theory]
    [InlineData(ColorMode.Rgba)]
    [InlineData(ColorMode.Indexed)]
    public void RoundTrip_Bytes_ModelledFieldsEqual(ColorMode mode)
    {
        var sp = Build(mode);
        var warnings = new List<string>();
        var bytes = Save(sp, warnings);
        AseHand.Parse(bytes); // every size field is right
        var r = Load(bytes, warnings);
        Assert.Empty(warnings);

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
        Assert.Equal(sp.Tags.Select(t => (t.Name, t.From, t.To, t.Dir, t.Color)), r.Tags.Select(t => (t.Name, t.From, t.To, t.Dir, t.Color)));
        Assert.Equal(sp.Cels.Count, r.Cels.Count);
        foreach (var ((layer, frame), cel) in sp.Cels)
        {
            var rc = r.Cels[(r.Layers[sp.Layers.IndexOf(layer)], frame)];
            Assert.Equal((cel.X, cel.Y, cel.Opacity), (rc.X, rc.Y, rc.Opacity));
            Assert.Equal(cel.Image.Pixels, rc.Image.Pixels);
        }
        Assert.Empty(r.UnknownChunks);

        Assert.Equal(bytes, Save(r)); // writing what was read gives the same file
    }

    [Fact]
    public void HandBuilt_File_ReadsLayersGroupsCelsAndTags()
    {
        var red = new Rgba(255, 0, 0, 255);
        var px = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 2x1 RGBA
        var file = AseHand.File(4, 3, 32, flags: 3, transparent: 0, ncolors: 1,
            AseHand.Frame(70,
                AseHand.Palette(red),
                AseHand.Layer("Bottom", flags: 1),             // visible, not editable → locked
                AseHand.Layer("G", type: 1, blend: 2, opacity: 77),
                AseHand.Layer("In", level: 1, flags: 2, opacity: 10),
                AseHand.RawCel(0, -1, 2, 255, 2, 1, px),
                AseHand.ZCel(2, 3, 0, 51, 2, 1, px),
                AseHand.Tags((0, 1, 1, new Rgba(10, 20, 30, 0), "run"))),
            AseHand.Frame(90));
        var sp = Load(file);
        Assert.Equal((4, 3, ColorMode.Rgba), (sp.Width, sp.Height, sp.Mode));
        Assert.Equal(new[] { red }, sp.Palette.Colors);
        Assert.Equal(new[] { "Bottom", "G", "In" }, sp.Layers.Select(l => l.Name));
        Assert.True(sp.Layers[0].Locked);
        Assert.True(sp.Layers[1].IsGroup);
        Assert.Equal((BlendMode.Screen, (byte)77), (sp.Layers[1].Blend, sp.Layers[1].Opacity));
        Assert.Same(sp.Layers[1], sp.Layers[2].Parent);
        Assert.False(sp.Layers[2].Visible);
        Assert.Equal((byte)10, sp.Layers[2].Opacity);
        Assert.Equal(new[] { 70, 90 }, sp.Frames.Select(f => f.DurationMs));
        var c0 = sp.Cels[(sp.Layers[0], 0)];
        Assert.Equal((-1, 2, 1f), (c0.X, c0.Y, c0.Opacity));
        Assert.Equal(new[] { 0x04030201u, 0x08070605u }, c0.Image.Pixels);
        var c2 = sp.Cels[(sp.Layers[2], 0)];
        Assert.Equal((3, 0, 0.2f), (c2.X, c2.Y, c2.Opacity));
        Assert.Equal(c0.Image.Pixels, c2.Image.Pixels);
        var tag = Assert.Single(sp.Tags);
        Assert.Equal(("run", 0, 1, LoopDir.Reverse, new Rgba(10, 20, 30, 255)), (tag.Name, tag.From, tag.To, tag.Dir, tag.Color));
    }

    [Fact]
    public void LinkedCel_PreservedAsLink()
    {
        var px = new byte[] { 1, 2, 3, 255 };
        var file = AseHand.File(1, 1, 32, 1, 0, 1,
            AseHand.Frame(100, AseHand.Layer("L"), AseHand.ZCel(0, 0, 0, 255, 1, 1, px)),
            AseHand.Frame(100, AseHand.LinkCel(0, 0)));
        var sp = Load(file);
        var layer = sp.Layers[0];
        Assert.Same(sp.Cels[(layer, 0)], sp.Cels[(layer, 1)]);

        var frames = AseHand.Parse(Save(sp));
        var cel1 = Assert.Single(frames[1], c => c.Type == 0x2005).Data;
        Assert.Equal(1, AseHand.U16At(cel1, 7));  // cel type: linked
        Assert.Equal(0, AseHand.U16At(cel1, 16)); // linked to frame 0
        var cel0 = Assert.Single(frames[0], c => c.Type == 0x2005).Data;
        Assert.Equal(2, AseHand.U16At(cel0, 7));  // compressed image
    }

    [Fact]
    public void Indexed_TransparentIndex_Kept()
    {
        var file = AseHand.File(3, 1, 8, 1, transparent: 2, ncolors: 3,
            AseHand.Frame(100,
                AseHand.Palette(new Rgba(1, 1, 1, 255), new Rgba(2, 2, 2, 255), new Rgba(3, 3, 3, 255)),
                AseHand.Layer("L"),
                AseHand.RawCel(0, 0, 0, 255, 3, 1, new byte[] { 0, 2, 1 })));
        var sp = Load(file);
        Assert.Equal(ColorMode.Indexed, sp.Mode);
        Assert.Equal(2, sp.Palette.TransparentIndex);
        Assert.Equal(new uint[] { 0, 2, 1 }, sp.Cels[(sp.Layers[0], 0)].Image.Pixels);

        var bytes = Save(sp);
        Assert.Equal(8, AseHand.U16At(bytes, 12)); // depth
        Assert.Equal(2, bytes[28]);                 // transparent index in the header
        var back = Load(bytes);
        Assert.Equal(2, back.Palette.TransparentIndex);
        Assert.Equal(new uint[] { 0, 2, 1 }, back.Cels[(back.Layers[0], 0)].Image.Pixels);
    }

    private static byte[] FileWithUnknownChunks()
    {
        var profile = AseHand.Chunk(0x2007, b => b.U16(1).U16(0).U32(0).Zero(8));
        var celExtra = AseHand.Chunk(0x2006, b => b.U32(1).U32(0x10000).U32(0x20000).U32(0x30000).U32(0x40000).Zero(16));
        var slice = AseHand.Chunk(0x2022, b => b.U32(1).U32(0).U32(0).Str("hit").U32(0).S16(0).U32(0).U32(0).U32(2).U32(2));
        var future = AseHand.Chunk(0x2099, b => b.U32(0xDEADBEEF));
        return AseHand.File(2, 2, 32, 1, 0, 2,
            AseHand.Frame(100,
                profile,
                AseHand.Palette(new Rgba(0, 0, 0, 255), new Rgba(9, 9, 9, 100)), // alpha → no old palette chunk
                AseHand.Layer("L"),
                AseHand.UserData("layer note", null),
                AseHand.RawCel(0, 0, 0, 255, 1, 1, new byte[] { 1, 2, 3, 4 }),
                celExtra,
                AseHand.Tags((0, 1, 0, new Rgba(50, 60, 70, 0), "t")), // Aseprite 1.3 writes the colour in both places
                AseHand.UserData("tag note", new Rgba(50, 60, 70, 80)),
                slice),
            AseHand.Frame(100,
                AseHand.RawCel(0, 0, 0, 255, 1, 1, new byte[] { 5, 6, 7, 8 }),
                future));
    }

    [Fact]
    public void UnknownChunks_WrittenBack()
    {
        var file = FileWithUnknownChunks();
        var original = AseHand.Parse(file);
        var warnings = new List<string>();
        var sp = Load(file, warnings);
        Assert.Empty(warnings);
        Assert.NotEmpty(sp.UnknownChunks);
        Assert.Equal(new Rgba(50, 60, 70, 80), sp.Tags[0].Color); // the tag's user-data colour wins

        var bytes = Save(sp, warnings);
        Assert.Empty(warnings);
        var written = AseHand.Parse(bytes);
        Assert.Equal(original.Count, written.Count);
        for (int f = 0; f < original.Count; f++)
        {
            Assert.Equal(original[f].Select(c => c.Type), written[f].Select(c => c.Type));
            for (int k = 0; k < original[f].Count; k++)
                if (original[f][k].Type != 0x2005) // cels are recompressed
                    Assert.Equal(original[f][k].Data, written[f][k].Data);
        }

        // A changed tag colour is written into the tag's user data, keeping its text.
        sp.Tags[0].Color = new Rgba(1, 2, 3, 4);
        var again = Load(Save(sp));
        Assert.Equal(new Rgba(1, 2, 3, 4), again.Tags[0].Color);

        // Deleting the frame that held an unknown chunk drops it with a warning.
        sp.Frames.RemoveAt(1);
        sp.Cels.Remove((sp.Layers[0], 1));
        var lost = new List<string>();
        var cut = AseHand.Parse(Save(sp, lost));
        Assert.Contains(AsepriteFormat.LostDataWarning, lost);
        Assert.Equal("Some Aseprite data (slices/user data/tilesets) could not be kept", AsepriteFormat.LostDataWarning);
        Assert.DoesNotContain(cut.SelectMany(f => f), c => c.Type == 0x2099);
        Assert.Contains(cut[0], c => c.Type == 0x2022);
    }

    [Fact]
    public void Truncated_ReadableError()
    {
        var bytes = Save(Build(ColorMode.Rgba));
        for (int n = 0; n < bytes.Length; n += Math.Max(1, bytes.Length / 60))
            Assert.Throws<FormatException>(() => Load(bytes[..n]));
        var mid = Assert.Throws<FormatException>(() => Load(bytes[..(bytes.Length / 2)]));
        Assert.Contains("damaged", mid.Message);
        var notAse = Assert.Throws<FormatException>(() => Load(Encoding.ASCII.GetBytes(new string('x', 300))));
        Assert.Contains("not an Aseprite file", notAse.Message);

        // A cel whose compressed data is garbage.
        var bad = AseHand.File(1, 1, 32, 1, 0, 1, AseHand.Frame(100, AseHand.Layer("L"),
            AseHand.Chunk(0x2005, b => b.U16(0).S16(0).S16(0).U8(255).U16(2).S16(0).Zero(5).U16(1).U16(1).U32(0x12345678))));
        Assert.Throws<FormatException>(() => Load(bad));
    }

    [Fact]
    public void GreyscaleMode_ReadsAsRgba_WithWarning()
    {
        var file = AseHand.File(2, 1, 16, 1, 0, 1,
            AseHand.Frame(100, AseHand.Layer("L"), AseHand.RawCel(0, 0, 0, 255, 2, 1, new byte[] { 10, 255, 200, 7 })));
        var warnings = new List<string>();
        var sp = Load(file, warnings);
        Assert.Contains("Greyscale sprite converted to RGBA", warnings);
        Assert.Equal(ColorMode.Rgba, sp.Mode);
        Assert.Equal(new[] { new Rgba(10, 10, 10, 255).Packed, new Rgba(200, 200, 200, 7).Packed }, sp.Cels[(sp.Layers[0], 0)].Image.Pixels);
        Assert.Equal(32, AseHand.U16At(Save(sp), 12)); // never written as greyscale
    }

    [Fact]
    public void OldPalette_UsedOnlyWithoutNewPalette()
    {
        var old = AseHand.Chunk(0x0004, b => b.U16(1).U8(0).U8(2).U8(1).U8(2).U8(3).U8(4).U8(5).U8(6));
        var old64 = AseHand.Chunk(0x0011, b => b.U16(1).U8(1).U8(1).U8(63).U8(0).U8(32));
        var onlyOld = Load(AseHand.File(1, 1, 32, 1, 0, 2, AseHand.Frame(100, old, old64, AseHand.Layer("L"))));
        Assert.Equal(new[] { new Rgba(1, 2, 3, 255), new Rgba(255, 0, 130, 255) }, onlyOld.Palette.Colors);

        var both = Load(AseHand.File(1, 1, 32, 1, 0, 1, AseHand.Frame(100, old, AseHand.Palette(new Rgba(9, 9, 9, 9)), AseHand.Layer("L"))));
        Assert.Equal(new[] { new Rgba(9, 9, 9, 9) }, both.Palette.Colors);
    }

    [Fact]
    public void UnmodelledLayerFlags_Kept()
    {
        // Background (8) + visible + editable, and a hidden, locked, collapsed (32) group.
        var file = AseHand.File(1, 1, 32, 1, 0, 1, AseHand.Frame(100, AseHand.Layer("Background", flags: 11), AseHand.Layer("G", flags: 32, type: 1)));
        var sp = Load(file);
        sp.Layers[1].Visible = true; // a modelled change still applies
        var layers = AseHand.Parse(Save(sp))[0].Where(c => c.Type == 0x2004).Select(c => AseHand.U16At(c.Data, 0));
        Assert.Equal(new[] { 11, 33 }, layers);
    }

    [Fact]
    public void Registry_FindsAsepriteExtensions()
    {
        Assert.IsType<AsepriteFormat>(FormatRegistry.ForPath("a/x.aseprite"));
        Assert.IsType<AsepriteFormat>(FormatRegistry.ForPath("x.ASE"));
    }
}
