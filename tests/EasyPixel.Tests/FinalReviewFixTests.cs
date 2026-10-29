using System.Text.Json;
using EasyPixel.Core.Bridge;
using EasyPixel.Core.Formats;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Core.Select;
using EasyPixel.Core.Update;

namespace EasyPixel.Tests;

/// <summary>Fixes from the final v0.1.0 review: palette history, transparent entry, linked cels in .epx, export order, reader rules.</summary>
public sealed class FinalReviewFixTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EP_Final_" + Guid.NewGuid().ToString("N")[..8]);

    public FinalReviewFixTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static Sprite Indexed(int extraColours)
    {
        var sp = new Sprite(4, 4, ColorMode.Indexed); // index 0 = transparent
        for (int i = 0; i < extraColours; i++) sp.Palette.Colors.Add(new Rgba((byte)(10 * i), 50, 50, 255));
        sp.GetOrCreateCel(sp.Layers[0], 0);
        return sp;
    }

    // ---- palette growth is part of the undo step ----

    [Fact]
    public void CompoundEdit_PaletteGrowth_UndoRemovesEntries_RedoAcrossRemoveColour_KeepsIndicesValid()
    {
        var sp = Indexed(16); // 17 entries
        var h = new History(sp);
        var cel = sp.Cels[(sp.Layers[0], 0)];

        h.Push(SpriteStateEdit.Perform(sp, "Remove colour", () => sp.RemovePaletteEntry(5))); // 16 entries

        // a stroke in a new colour: the palette grows inside the step
        var pal = PaletteSnapshot.Take(sp);
        var before = PixelEdit.Snapshot(cel.Image, new PixelRect(1, 1, 1, 1));
        uint v = sp.ColorToPixel(new Rgba(1, 2, 3, 255));
        Assert.Equal(16u, v);
        cel.Image.Set(1, 1, v);
        h.Push(CompoundEdit.WithPalette(PixelEdit.Capture(cel, new PixelRect(1, 1, 1, 1), before, "Pencil"), pal, sp));

        h.Undo();
        Assert.Equal(16, sp.Palette.Colors.Count); // the added entry went with the stroke
        h.Undo();
        Assert.Equal(17, sp.Palette.Colors.Count);
        h.Redo();
        h.Redo();
        Assert.Equal(17, sp.Palette.Colors.Count);
        Assert.Equal(16u, cel.Image.Get(1, 1));
        Assert.All(cel.Image.Pixels, p => Assert.True(p < (uint)sp.Palette.Colors.Count));
        Assert.Equal(new Rgba(1, 2, 3, 255), sp.Palette.Colors[16]);
    }

    [Fact]
    public void CompoundEdit_WithoutPaletteChange_IsThePlainEdit()
    {
        var sp = Indexed(2);
        var cel = sp.Cels[(sp.Layers[0], 0)];
        var pal = PaletteSnapshot.Take(sp);
        var e = CelReplaceEdit.Capture(cel, CelReplaceEdit.Begin(cel), "Move");
        Assert.Same(e, CompoundEdit.WithPalette(e, pal, sp));
        Assert.Same(e, CompoundEdit.WithPalette(e, null, sp));
    }

    // ---- the transparent entry of an indexed sprite ----

    [Fact]
    public void RemovePaletteEntry_Transparent_IsRefused_AndOpaqueNeverMapsToTransparent()
    {
        var sp = Indexed(2); // 0 transparent, 1 and 2 opaque
        Assert.Throws<InvalidOperationException>(() => sp.RemovePaletteEntry(0));
        var cel = sp.Cels[(sp.Layers[0], 0)];
        cel.Image.Set(0, 0, 2);
        sp.Palette.Colors[2] = Rgba.Transparent with { A = 1 }; // nearer to the transparent entry than to colour 1
        sp.RemovePaletteEntry(2);
        Assert.Equal(1u, cel.Image.Get(0, 0));
        Assert.Equal(0, sp.Palette.TransparentIndex);
    }

    [Fact]
    public void RemovePaletteEntry_Rgba_OnlyShiftsTheList()
    {
        var sp = new Sprite(2, 2);
        sp.Palette.Colors.AddRange(new[] { new Rgba(1, 0, 0, 255), new Rgba(2, 0, 0, 255), new Rgba(3, 0, 0, 255) });
        sp.Palette.TransparentIndex = 2;
        sp.GetOrCreateCel(sp.Layers[0], 0).Image.Set(0, 0, 12345);
        sp.RemovePaletteEntry(0);
        Assert.Equal(2, sp.Palette.Colors.Count);
        Assert.Equal(1, sp.Palette.TransparentIndex);
        Assert.Equal(12345u, sp.Cels[(sp.Layers[0], 0)].Image.Get(0, 0));
    }

    [Fact]
    public void EnsureTransparentIndex_UsesAnAlphaZeroEntry_OrAddsOne_OrGivesUpWhenFull()
    {
        var sp = Indexed(2);
        sp.Palette.TransparentIndex = -1;
        Assert.True(sp.EnsureTransparentIndex());
        Assert.Equal(0, sp.Palette.TransparentIndex); // index 0 already had alpha 0
        Assert.False(sp.EnsureTransparentIndex());

        sp.Palette.Colors[0] = new Rgba(9, 9, 9, 255);
        sp.Palette.TransparentIndex = -1;
        Assert.True(sp.EnsureTransparentIndex());
        Assert.Equal(3, sp.Palette.TransparentIndex);
        Assert.Equal(Rgba.Transparent, sp.Palette.Colors[3]);
        Assert.Equal(3u, sp.TransparentPixel);

        var full = new Sprite(1, 1, ColorMode.Indexed);
        full.Palette.Colors.Clear();
        for (int i = 0; i < Palette.MaxColors; i++) full.Palette.Colors.Add(new Rgba((byte)i, 0, 0, 255));
        full.Palette.TransparentIndex = -1;
        Assert.False(full.EnsureTransparentIndex());
        Assert.False(new Sprite(1, 1).EnsureTransparentIndex()); // RGBA has no palette indices
    }

    [Fact]
    public void ColorToPixel_FullPalette_NeverPicksTheTransparentEntry()
    {
        var sp = new Sprite(1, 1, ColorMode.Indexed);
        sp.Palette.Colors[0] = new Rgba(0, 0, 0, 0);
        for (int i = 1; i < Palette.MaxColors; i++) sp.Palette.Colors.Add(new Rgba(255, 255, (byte)i, 255));
        Assert.NotEqual(0u, sp.ColorToPixel(new Rgba(0, 0, 0, 255)));
    }

    // ---- .epx ----

    [Fact]
    public void Epx_LinkedCels_StayLinked_AndArePictureOnce()
    {
        var sp = new Sprite(3, 2);
        for (int i = 0; i < 3; i++) sp.Frames.Add(new Frame());
        var l = sp.Layers[0];
        var shared = sp.GetOrCreateCel(l, 0);
        shared.Image.Set(1, 1, new Rgba(9, 8, 7, 255).Packed);
        sp.Cels[(l, 2)] = shared;
        sp.Cels[(l, 3)] = shared;
        sp.GetOrCreateCel(l, 1).Image.Set(0, 0, 5);

        var ms = new MemoryStream();
        new EpxFormat().Write(sp, ms, new List<string>());
        ms.Position = 0;
        using (var zip = new System.IO.Compression.ZipArchive(new MemoryStream(ms.ToArray())))
            Assert.Equal(2, zip.Entries.Count(e => e.FullName.StartsWith("cels/")));

        var back = new EpxFormat().Read(ms, new List<string>());
        var bl = back.Layers[0];
        Assert.Same(back.Cels[(bl, 0)], back.Cels[(bl, 2)]);
        Assert.Same(back.Cels[(bl, 0)], back.Cels[(bl, 3)]);
        Assert.NotSame(back.Cels[(bl, 0)], back.Cels[(bl, 1)]);
        Assert.Equal(new Rgba(9, 8, 7, 255).Packed, back.Cels[(bl, 3)].Image.Get(1, 1));
    }

    private static Sprite ReadEpxWithJson(Func<string, string> edit, List<string> warnings, ColorMode mode = ColorMode.Rgba)
    {
        var sp = new Sprite(2, 2, mode);
        sp.Tags.Add(new Tag { Name = "t", From = 0, To = 0 });
        var ms = new MemoryStream();
        new EpxFormat().Write(sp, ms, new List<string>());
        var bytes = ms.ToArray();
        var outMs = new MemoryStream();
        using (var src = new System.IO.Compression.ZipArchive(new MemoryStream(bytes)))
        using (var dst = new System.IO.Compression.ZipArchive(outMs, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            foreach (var e in src.Entries)
            {
                using var r = e.Open();
                var data = new MemoryStream();
                r.CopyTo(data);
                var payload = data.ToArray();
                if (e.FullName == "sprite.json") payload = System.Text.Encoding.UTF8.GetBytes(edit(System.Text.Encoding.UTF8.GetString(payload)));
                using var w = dst.CreateEntry(e.FullName).Open();
                w.Write(payload);
            }
        outMs.Position = 0;
        return new EpxFormat().Read(outMs, warnings);
    }

    [Fact]
    public void Epx_NumericOrUnknownEnums_FallBackWithAWarning_AndTagsAreClamped()
    {
        var warnings = new List<string>();
        var sp = ReadEpxWithJson(j => j.Replace("\"Normal\"", "\"99\"").Replace("\"Forward\"", "\"Sideways\"")
            .Replace("\"from\": 0", "\"from\": -4").Replace("\"to\": 0", "\"to\": 50"), warnings);
        Assert.Equal(BlendMode.Normal, sp.Layers[0].Blend);
        Assert.Equal(LoopDir.Forward, sp.Tags[0].Dir);
        Assert.Equal((0, 0), (sp.Tags[0].From, sp.Tags[0].To));
        Assert.Equal(new[] { EpxFormat.UnknownValueWarning }, warnings);
    }

    [Fact]
    public void Epx_IndexedWithoutTransparentIndex_GetsOne()
    {
        var sp = ReadEpxWithJson(j => j.Replace("\"transparentIndex\": 0", "\"transparentIndex\": -1"), new List<string>(), ColorMode.Indexed);
        Assert.True(sp.Palette.TransparentIndex >= 0);
        Assert.Equal(0, sp.Palette.Colors[sp.Palette.TransparentIndex].A);
    }

    // ---- reduce colours counts cel by cel ----

    [Fact]
    public void MedianCut_FromCounts_MatchesMedianCut_FromImage()
    {
        var img = new Image(8, 8);
        var rnd = new Random(3);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = new Rgba((byte)rnd.Next(256), (byte)rnd.Next(256), 0, 255).Packed;
        var counts = new Dictionary<uint, int>();
        Quantize.CountColors(img, counts);
        Assert.Equal(Quantize.MedianCut(img, 5).Colors, Quantize.MedianCut(counts, 5).Colors);
    }

    // ---- floating transforms keep the centre ----

    [Fact]
    public void Floating_RotateAndScale_KeepTheirCentre()
    {
        var f = new Floating(new Image(4, 2), 10, 10);
        f.Rotate90();
        Assert.Equal((2, 4, 11, 9), (f.Pixels.Width, f.Pixels.Height, f.X, f.Y));
        f.Rotate90(clockwise: false);
        Assert.Equal((4, 2, 10, 10), (f.Pixels.Width, f.Pixels.Height, f.X, f.Y));
        f.Scale(8, 4);
        Assert.Equal((8, 4, 8, 9), (f.Pixels.Width, f.Pixels.Height, f.X, f.Y));

        var img = new Image(2, 1);
        img.Set(0, 0, 7);
        var g = new Floating(img, 0, 0);
        g.Rotate90(clockwise: false); // counter-clockwise: the left pixel goes to the bottom
        Assert.Equal(7u, g.Pixels.Get(0, 1));
    }

    [Fact]
    public void Selection_Offset_MovesAndResizes()
    {
        var s = Selection.None(4, 4);
        s.Set(1, 1, true);
        s.Set(3, 3, true);
        var moved = s.Offset(1, 0);
        Assert.True(moved.Contains(2, 1));
        Assert.False(moved.Contains(3, 3)); // pushed off
        var bigger = s.Offset(5, 5, 10, 10);
        Assert.Equal((10, 10), (bigger.Width, bigger.Height));
        Assert.True(bigger.Contains(8, 8));
        Assert.Same(s, s.Offset(0, 0));
    }

    // ---- export to EasyEngine ----

    private static Sprite Numbered(int frames)
    {
        var sp = new Sprite(frames, 1);
        for (int f = 1; f < frames; f++) sp.Frames.Add(new Frame());
        for (int f = 0; f < frames; f++) sp.GetOrCreateCel(sp.Layers[0], f).Image.Set(f, 0, new Rgba(255, 0, 0, 255).Packed);
        return sp;
    }

    [Fact]
    public void Handoff_FrameOrder_FollowsTheTagDirection()
    {
        var sp = Numbered(5);
        Assert.Equal(new[] { 1, 2, 3 }, ExportHandoff.FrameOrder(sp, new Tag { From = 1, To = 3 }));
        Assert.Equal(new[] { 3, 2, 1 }, ExportHandoff.FrameOrder(sp, new Tag { From = 1, To = 3, Dir = LoopDir.Reverse }));
        Assert.Equal(new[] { 1, 2, 3, 2 }, ExportHandoff.FrameOrder(sp, new Tag { From = 1, To = 3, Dir = LoopDir.PingPong }));
        Assert.Equal(new[] { 1, 2 }, ExportHandoff.FrameOrder(sp, new Tag { From = 1, To = 2, Dir = LoopDir.PingPong }));
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ExportHandoff.FrameOrder(sp, null));
    }

    [Fact]
    public void Handoff_ReverseTag_WritesFramesBackwards_AndDuplicateNamesAreTwoExports()
    {
        var sp = Numbered(3);
        sp.Tags.Add(new Tag { Name = "go", From = 0, To = 2, Dir = LoopDir.Reverse });
        sp.Tags.Add(new Tag { Name = "go", From = 1, To = 1 });
        var folders = ExportHandoff.WriteAll(sp, "x", Path.Combine(_dir, "Exports"), null, new DateTime(2026, 1, 1));
        Assert.Equal(2, folders.Count);
        Assert.NotEqual(folders[0], folders[1]);
        var first = Png.Decode(File.ReadAllBytes(Path.Combine(folders[0], "frame_000.png")));
        Assert.NotEqual(0u, first.Get(2, 0)); // the reversed export starts with frame 2
        Assert.Single(Directory.GetFiles(folders[1], "frame_*.png"));
        var only = Png.Decode(File.ReadAllBytes(Path.Combine(folders[1], "frame_000.png")));
        Assert.NotEqual(0u, only.Get(1, 0)); // the second "go" is its own frames, not the first tag again
    }

    [Fact]
    public void Reader_MatchesEngineRules_StringFps_BlankName_Clamp()
    {
        // The same cases EasyEngine's fulltest checks on its copy of the reader (FullTest.EasyPixel.cs).
        var root = Path.Combine(_dir, "Exports");
        void Manifest(string folder, string json)
        {
            Directory.CreateDirectory(Path.Combine(root, folder));
            File.WriteAllText(Path.Combine(root, folder, ExportHandoff.ManifestName), json);
        }
        Manifest("strfps", "{\"name\":\"a\",\"fps\":\"6\",\"frames\":[\"frame_000.png\"]}");
        Manifest("blank", "{\"name\":\"  \",\"fps\":12,\"frames\":[]}");
        Manifest("fast", "{\"name\":\"c\",\"fps\":500,\"frames\":[]}");
        Manifest("zero", "{\"name\":\"d\",\"fps\":0,\"frames\":[]}");
        var byFolder = ExportHandoff.ListRecent(20, root).ToDictionary(i => Path.GetFileName(i.Folder));
        Assert.Equal(6, byFolder["strfps"].Fps);
        Assert.Equal("blank", byFolder["blank"].Name);
        Assert.Equal(60, byFolder["fast"].Fps);
        Assert.Equal(10, byFolder["zero"].Fps);
    }

    [Fact]
    public void ToSprite_HugeFps_NeverGivesZeroMilliseconds()
    {
        var png = Path.Combine(_dir, "f.png");
        File.WriteAllBytes(png, Png.Encode(new Image(2, 2, new Rgba(1, 2, 3, 255).Packed)));
        var src = new EngineSprite("", "Hero", new[] { new EngineAnimation("run", new[] { png }, 5000) }, new List<string>());
        var sp = EasyEngineBridge.ToSprite(src, new List<string>())!;
        Assert.Equal(1, sp.Frames[0].DurationMs);
    }

    // ---- updater ----

    [Fact]
    public void FilesToRemove_IgnoresASiblingFolderWithTheSamePrefix()
    {
        var install = Path.Combine(_dir, "App");
        var sibling = Path.Combine(_dir, "App2");
        Directory.CreateDirectory(install);
        Directory.CreateDirectory(sibling);
        File.WriteAllText(Path.Combine(install, "a.dll"), "");
        File.WriteAllText(Path.Combine(sibling, "b.dll"), "");
        // "..\App2\b.dll" is filtered as a manifest entry; FilesToRemove must also refuse a path that only shares the prefix.
        var list = UpdateLogic.FilesToRemove(install + Path.DirectorySeparatorChar, "a.dll\n");
        Assert.Equal(new[] { Path.Combine(install, "a.dll") }, list);
        Assert.Empty(UpdateLogic.FilesToRemove(install, "..\\App2\\b.dll\n"));
    }
}
