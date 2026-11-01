using System.Buffers.Binary;
using System.Text;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

/// <summary>Palette files made by other programs (not EasyPixel's own writer): each format's other shapes.</summary>
public class PaletteFormatTests
{
    private static List<Rgba> Read(byte[] d, string ext) => PaletteFormats.Read(d, ext);

    [Fact]
    public void RiffPal_IsToldFromJasc_ByItsContent()
    {
        var d = new List<byte>();
        d.AddRange(Encoding.ASCII.GetBytes("RIFF")); d.AddRange(BitConverter.GetBytes(4 + 8 + 4 + 8));
        d.AddRange(Encoding.ASCII.GetBytes("PAL data")); d.AddRange(BitConverter.GetBytes(4 + 8));
        d.AddRange(new byte[] { 0, 3, 2, 0 }); // version 0x0300, 2 colours
        d.AddRange(new byte[] { 255, 0, 0, 0, 10, 20, 30, 0 });
        Assert.Equal(new[] { new Rgba(255, 0, 0, 255), new Rgba(10, 20, 30, 255) }, Read(d.ToArray(), ".pal"));
    }

    [Fact]
    public void PaintNet_ReadsAlphaAndSkipsComments()
    {
        var c = Read(Encoding.UTF8.GetBytes("; paint.net Palette File\n;comment\nFFFF0000\n80112233\n"), ".txt");
        Assert.Equal(new[] { new Rgba(255, 0, 0, 255), new Rgba(0x11, 0x22, 0x33, 0x80) }, c);
    }

    [Fact]
    public void Act_WithoutTheCount_HasAll256_AndTheTransparentOneHasNoAlpha()
    {
        var d = new byte[772];
        d[3] = 9;
        BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(768), 3);
        BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(770), 2);
        var c = Read(d, ".act");
        Assert.Equal(3, c.Count);
        Assert.Equal(new Rgba(9, 0, 0, 255), c[1]);
        Assert.Equal(0, c[2].A);
        Assert.Equal(256, Read(new byte[768], ".act").Count);
    }

    [Fact]
    public void Aco_Version2WithNames_CmykAndGrey()
    {
        var d = new List<byte>();
        void U16(int v) { d.Add((byte)(v >> 8)); d.Add((byte)v); }
        U16(2); U16(2);
        U16(2); U16(0xFFFF); U16(0); U16(0xFFFF); U16(0xFFFF); // CMYK: no cyan, full magenta → (255, 0, 255)
        d.AddRange(new byte[] { 0, 0, 0, 2 }); U16('M'); U16(0);  // name "M"
        U16(8); U16(10000); U16(0); U16(0); U16(0);                 // grey: 10000 = black
        d.AddRange(new byte[] { 0, 0, 0, 1 }); U16(0);
        Assert.Equal(new[] { new Rgba(255, 0, 255, 255), new Rgba(0, 0, 0, 255) }, Read(d.ToArray(), ".aco"));
    }

    [Fact]
    public void AdobeAse_ReadsCmykGreyAndLab_SkippingGroups()
    {
        var d = new List<byte>(Encoding.ASCII.GetBytes("ASEF"));
        void U16(int v) { d.Add((byte)(v >> 8)); d.Add((byte)v); }
        void U32(int v) { U16(v >> 16); U16(v & 0xFFFF); }
        void F(float v) { var b = new byte[4]; BinaryPrimitives.WriteSingleBigEndian(b, v); d.AddRange(b); }
        void Colour(string model, params float[] vals) { U16(1); U32(2 + 2 + 4 + vals.Length * 4 + 2); U16(1); U16(0); d.AddRange(Encoding.ASCII.GetBytes(model)); foreach (var v in vals) F(v); U16(2); }
        U16(1); U16(0); U32(5);
        U16(0xC001); U32(4); U16(1); U16(0); // group start, empty name
        Colour("CMYK", 0, 0, 0, 1);           // black
        Colour("Gray", 1);                    // white
        Colour("LAB ", 1, 0, 0);              // L = 100%: white
        U16(0xC002); U32(0);                  // group end
        var c = Read(d.ToArray(), ".ase");
        Assert.Equal(new Rgba(0, 0, 0, 255), c[0]);
        Assert.Equal(new Rgba(255, 255, 255, 255), c[1]);
        Assert.True(c[2].R > 250 && c[2].G > 250 && c[2].B > 250);
    }

    [Fact]
    public void AsepriteAse_GivesTheSpritesPalette()
    {
        var s = new Sprite(4, 4, ColorMode.Indexed);
        s.Palette.Colors.AddRange(new[] { new Rgba(0, 0, 0, 0), new Rgba(1, 2, 3, 255), new Rgba(200, 100, 50, 255) });
        s.Palette.TransparentIndex = 0;
        var ms = new MemoryStream();
        new AsepriteFormat().Write(s, ms, new List<string>());
        Assert.Equal(new[] { new Rgba(1, 2, 3, 255), new Rgba(200, 100, 50, 255) }, Read(ms.ToArray(), ".ase"));
    }

    [Fact]
    public void Picture_GivesItsColours_InTheOrderTheyAppear()
    {
        var img = new Image(3, 1);
        img.Set(0, 0, new Rgba(9, 9, 9, 255).Packed);
        img.Set(1, 0, new Rgba(200, 0, 0, 255).Packed);
        img.Set(2, 0, new Rgba(9, 9, 9, 255).Packed);
        Assert.Equal(new[] { new Rgba(9, 9, 9, 255), new Rgba(200, 0, 0, 255) }, Read(Png.Encode(img), ".png"));
    }

    [Fact]
    public void BrokenFiles_SayFormatException()
    {
        Assert.Throws<FormatException>(() => Read(new byte[10], ".act"));
        Assert.Throws<FormatException>(() => Read(new byte[] { 0, 9, 0, 0 }, ".aco"));
        Assert.Throws<FormatException>(() => Read(Encoding.ASCII.GetBytes("ASEF"), ".ase"));
        Assert.Throws<FormatException>(() => Read(Encoding.ASCII.GetBytes("nope"), ".png"));
    }
}
