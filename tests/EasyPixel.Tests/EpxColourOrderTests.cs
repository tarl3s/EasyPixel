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
}
