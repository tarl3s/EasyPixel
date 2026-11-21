using System.IO.Compression;
using System.Text;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

/// <summary>.epx colours are written "RRGGBBAA" (format 2); format 1 files (the packed "AABBGGRR") still open the same.</summary>
public class EpxColourOrderTests
{
    private static readonly Rgba Plum = new(0x58, 0x47, 0x4B, 255), Wine = new(0x3C, 0x2C, 0x43, 255);

    private static (byte[] File, string Json) Save(Sprite s)
    {
        var ms = new MemoryStream();
        new EpxFormat().Write(s, ms, new List<string>());
        using var zip = new ZipArchive(new MemoryStream(ms.ToArray()));
        using var r = new StreamReader(zip.GetEntry("sprite.json")!.Open());
        return (ms.ToArray(), r.ReadToEnd());
    }

    private static Sprite TwoColours()
    {
        var s = new Sprite(2, 2);
        s.Palette.Colors.AddRange(new[] { Plum, Wine });
        s.Tags.Add(new Tag { Name = "walk", From = 0, To = 0, Color = Plum });
        return s;
    }

    [Fact]
    public void Colours_AreWrittenAsRRGGBBAA_LikeEveryOtherTool()
    {
        var json = Save(TwoColours()).Json;
        Assert.Contains("\"58474BFF\"", json);
        Assert.Contains("\"3C2C43FF\"", json);
        Assert.DoesNotContain("FF4B4758", json); // the old, red/blue-swapped way
        Assert.Contains("\"format\":2", json.Replace(" ", ""));
    }

    [Fact]
    public void RoundTrip_KeepsColours()
    {
        var back = new EpxFormat().Read(new MemoryStream(Save(TwoColours()).File), new List<string>());
        Assert.Equal(new[] { Plum, Wine }, back.Palette.Colors);
        Assert.Equal(Plum, back.Tags[0].Color);
    }

    [Fact]
    public void Format1File_StillOpensWithTheRightColours()
    {
        var (file, json) = Save(TwoColours());
        // turn it into what EasyPixel 0.3.0 and older wrote: format 1, colours as the packed value
        var old = json.Replace("\"58474BFF\"", "\"" + Plum.Packed.ToString("X8") + "\"")
                      .Replace("\"3C2C43FF\"", "\"" + Wine.Packed.ToString("X8") + "\"");
        old = System.Text.RegularExpressions.Regex.Replace(old, @"""format"":\s*2", "\"format\":1");
        var ms = new MemoryStream();
        ms.Write(file);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry("sprite.json")!.Delete();
            using var w = new StreamWriter(zip.CreateEntry("sprite.json").Open(), new UTF8Encoding(false));
            w.Write(old);
        }
        var back = new EpxFormat().Read(new MemoryStream(ms.ToArray()), new List<string>());
        Assert.Equal(new[] { Plum, Wine }, back.Palette.Colors);
        Assert.Equal(Plum, back.Tags[0].Color);
    }

    /// <summary>A format 1 file whose colours were written by an outside tool as "AARRGGBB" ("FF" + "RRGGBB").</summary>
    private static Sprite OpenFormat1(Sprite s, Func<Rgba, string> hex)
    {
        var (file, json) = Save(s);
        var old = json.Replace("\"58474BFF\"", "\"" + hex(Plum) + "\"").Replace("\"3C2C43FF\"", "\"" + hex(Wine) + "\"");
        old = System.Text.RegularExpressions.Regex.Replace(old, @"""format"":\s*2", "\"format\":1");
        var ms = new MemoryStream();
        ms.Write(file);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry("sprite.json")!.Delete();
            using var w = new StreamWriter(zip.CreateEntry("sprite.json").Open(), new UTF8Encoding(false));
            w.Write(old);
        }
        return new EpxFormat().Read(new MemoryStream(ms.ToArray()), new List<string>());
    }

    private static Sprite PaintedWith(params Rgba[] colours)
    {
        var s = TwoColours();
        s.Tags.Clear();
        var img = new Image(2, 2);
        s.Cels[(s.Layers[0], 0)] = new Cel(img, 0, 0);
        for (int i = 0; i < colours.Length; i++) img.Pixels[i] = colours[i].Packed;
        return s;
    }

    private static string Argb(Rgba c) => $"FF{c.R:X2}{c.G:X2}{c.B:X2}";

    [Fact]
    public void Format1File_WrittenAsAARRGGBB_ByAnotherTool_OpensWithTheRightPalette()
    {
        var back = OpenFormat1(PaintedWith(Plum, Wine), Argb);
        Assert.Equal(new[] { Plum, Wine }, back.Palette.Colors);
    }

    [Fact]
    public void Format1File_FromEasyPixel_IsNotSwappedBack()
    {
        var back = OpenFormat1(PaintedWith(Plum, Wine), c => c.Packed.ToString("X8"));
        Assert.Equal(new[] { Plum, Wine }, back.Palette.Colors);
        // an empty picture gives no evidence: the stored meaning (packed) wins
        Assert.Equal(new[] { Plum, Wine }, OpenFormat1(PaintedWith(), c => c.Packed.ToString("X8")).Palette.Colors);
    }
}
