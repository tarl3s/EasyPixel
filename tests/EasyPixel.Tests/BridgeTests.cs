using System.Text.Json;
using EasyPixel.Core.Bridge;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public sealed class BridgeTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EP_Bridge_" + Guid.NewGuid().ToString("N")[..8]);

    public BridgeTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private string P(params string[] parts) => Path.Combine(new[] { _dir }.Concat(parts).ToArray());

    private static Sprite Walk(params int[] durations)
    {
        var sp = new Sprite(4, 3);
        for (int f = 1; f < durations.Length; f++) sp.Frames.Add(new Frame());
        for (int f = 0; f < durations.Length; f++)
        {
            sp.Frames[f].DurationMs = durations[f];
            sp.GetOrCreateCel(sp.Layers[0], f).Image.Set(f % 4, 0, new Rgba(200, (byte)(f * 20), 0, 255).Packed);
        }
        return sp;
    }

    private void WritePng(string rel, int w, int h, uint colour)
    {
        var img = new Image(w, h, colour);
        var path = P(rel.Split('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Png.Encode(img));
    }

    [Fact]
    public void Handoff_WriteThenList_RoundTrip()
    {
        var sp = Walk(100, 100, 100);
        var when = new DateTime(2026, 10, 28, 14, 5, 9);
        var folder = ExportHandoff.Write(sp, null, P("Exports"), "walk", "C:\\art\\walk.epx", when);

        Assert.Equal(P("Exports", "walk-20261028-140509"), folder);
        Assert.True(File.Exists(Path.Combine(folder, "frame_000.png")));
        Assert.True(File.Exists(Path.Combine(folder, "frame_002.png")));
        var back = Png.Decode(File.ReadAllBytes(Path.Combine(folder, "frame_001.png")));
        Assert.Equal((4, 3), (back.Width, back.Height));
        Assert.Equal(new Rgba(200, 20, 0, 255).Packed, back.Get(1, 0));

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "easypixel-export.json")));
        var r = doc.RootElement;
        Assert.Equal(1, r.GetProperty("format").GetInt32());
        Assert.Equal("walk", r.GetProperty("name").GetString());
        Assert.Equal(10, r.GetProperty("fps").GetInt32());
        Assert.Equal(4, r.GetProperty("width").GetInt32());
        Assert.Equal(3, r.GetProperty("height").GetInt32());
        Assert.Equal("C:\\art\\walk.epx", r.GetProperty("source").GetString());
        Assert.Equal(new[] { "frame_000.png", "frame_001.png", "frame_002.png" },
            r.GetProperty("frames").EnumerateArray().Select(e => e.GetString()).ToArray());

        var list = ExportHandoff.ListRecent(20, P("Exports"));
        var info = Assert.Single(list);
        Assert.Equal((folder, "walk", 10, 3), (info.Folder, info.Name, info.Fps, info.FrameCount));
    }

    [Fact]
    public void Handoff_ListRecent_NewestFirst_SkipsBrokenFolders()
    {
        var root = P("Exports");
        var a = ExportHandoff.Write(Walk(100), null, root, "a", null, new DateTime(2026, 1, 1, 10, 0, 0));
        var b = ExportHandoff.Write(Walk(100), null, root, "b", null, new DateTime(2026, 1, 1, 11, 0, 0));
        Directory.CreateDirectory(Path.Combine(root, "junk"));
        Directory.CreateDirectory(Path.Combine(root, "bad"));
        File.WriteAllText(Path.Combine(root, "bad", "easypixel-export.json"), "{ not json");
        File.SetLastWriteTime(Path.Combine(a, "easypixel-export.json"), new DateTime(2026, 1, 1, 10, 0, 0));
        File.SetLastWriteTime(Path.Combine(b, "easypixel-export.json"), new DateTime(2026, 1, 1, 11, 0, 0));

        Assert.Equal(new[] { "b", "a" }, ExportHandoff.ListRecent(20, root).Select(i => i.Name));
        Assert.Single(ExportHandoff.ListRecent(1, root));
        Assert.Empty(ExportHandoff.ListRecent(20, P("nothing-here")));
    }

    [Fact]
    public void Handoff_FpsFromDurations()
    {
        Assert.Equal(10, ExportHandoff.FpsFor(new[] { 100 }));
        Assert.Equal(13, ExportHandoff.FpsFor(new[] { 50, 100 })); // average 75 ms = 13.33 fps
        Assert.Equal(10, ExportHandoff.FpsFor(new[] { 0, 0 }));    // nonsense durations fall back to 10

        var sp = Walk(50, 100, 100, 100);
        sp.Tags.Add(new Tag { Name = "run", From = 0, To = 1 });
        sp.Tags.Add(new Tag { Name = "idle", From = 2, To = 3 });
        var folders = ExportHandoff.WriteAll(sp, "hero", P("Exports"), null, new DateTime(2026, 2, 3, 4, 5, 6));
        Assert.Equal(2, folders.Count);
        var infos = ExportHandoff.ListRecent(20, P("Exports")).ToDictionary(i => i.Name);
        Assert.Equal(13, infos["run"].Fps);
        Assert.Equal(10, infos["idle"].Fps);
        Assert.Equal(2, infos["run"].FrameCount);
    }

    [Fact]
    public void Handoff_WholeTimelineWhenNoTags_AndSameSecondDoesNotCollide()
    {
        var when = new DateTime(2026, 2, 3, 4, 5, 6);
        var one = ExportHandoff.WriteAll(Walk(100, 100), "x", P("Exports"), null, when);
        var two = ExportHandoff.WriteAll(Walk(100, 100), "x", P("Exports"), null, when);
        Assert.NotEqual(one[0], two[0]);
        Assert.Equal(2, ExportHandoff.ListRecent(20, P("Exports")).Count);
        Assert.Throws<ArgumentException>(() => ExportHandoff.Write(Walk(100), "missing", P("Exports"), "x"));
    }

    [Fact]
    public void Handoff_NameWithBadCharacters_MakesAValidFolder()
    {
        var folder = ExportHandoff.Write(Walk(100), null, P("Exports"), "a/b:c?", null, new DateTime(2026, 1, 1));
        Assert.True(Directory.Exists(folder));
        Assert.StartsWith(P("Exports"), folder);
    }

    private string Project(string json)
    {
        var path = P("game", "p.eeproj");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void ListSprites_ReadsAnimationsAndSinglePictures()
    {
        WritePng("game/Assets/w1.png", 8, 8, 0xFF0000FF);
        WritePng("game/Assets/w2.png", 8, 8, 0xFF00FF00);
        WritePng("game/Assets/rock.png", 4, 4, 0xFFFFFFFF);
        var eeproj = Project("""
        {
          // comments and trailing commas are fine
          "Name": "Game",
          "Scenes": [ { "Name": "Level 1", "Layers": [ { "Name": "L", "Objects": [
            { "Id": "a", "Name": "Hero", "SpritePath": "Assets/rock.png",
              "Animations": [ { "Name": "walk", "Frames": ["Assets/w1.png", "Assets\\w2.png"], "Fps": 8, "Loop": true }, ] },
            { "Id": "b", "Name": "Rock", "SpritePath": "Assets/rock.png" },
            { "Id": "c", "Name": "Empty" },
          ] } ] } ],
          "Prefabs": [ { "Name": "Coin", "SpritePath": "Assets/rock.png" } ],
        }
        """);

        var sprites = EasyEngineBridge.ListSprites(eeproj);
        Assert.Equal(new[] { "Hero", "Rock", "Coin" }, sprites.Select(s => s.Object));
        var hero = sprites[0];
        Assert.Equal("Level 1", hero.Scene);
        var walk = Assert.Single(hero.Animations);
        Assert.Equal(("walk", 8, 2), (walk.Name, walk.Fps, walk.Frames.Count));
        Assert.All(walk.Frames, f => Assert.True(File.Exists(f)));
        Assert.Null(sprites[1].Animations[0].Name);
        Assert.Equal("", sprites[2].Scene); // prefab
    }

    [Fact]
    public void ListSprites_OldProjectMissingKeys_Works()
    {
        WritePng("game/a.png", 2, 2, 0xFF000000);
        Assert.Empty(EasyEngineBridge.ListSprites(Project("{}")));
        Assert.Empty(EasyEngineBridge.ListSprites(Project("""{"Scenes":[{"Layers":[{}]},{"Name":"S"}]}""")));
        var s = EasyEngineBridge.ListSprites(Project("""{"Scenes":[{"Layers":[{"Objects":[{"SpritePath":"a.png","Animations":[{"Frames":["a.png"]}],"Extra":5}]}]}]}"""));
        var only = Assert.Single(s);
        Assert.Equal(10, only.Animations[0].Fps);         // no Fps -> default
        Assert.False(string.IsNullOrEmpty(only.Object));  // no Name -> a stand-in
        Assert.ThrowsAny<IOException>(() => EasyEngineBridge.ListSprites(P("nope.eeproj")));
        Assert.Throws<InvalidDataException>(() => EasyEngineBridge.ListSprites(Project("[1,2")));
    }

    [Fact]
    public void ListSprites_MissingAssetFile_SkipsWithWarning()
    {
        WritePng("game/Assets/ok.png", 2, 2, 0xFF000000);
        var eeproj = Project("""
        {"Scenes":[{"Name":"S","Layers":[{"Objects":[
          {"Name":"Half","Animations":[{"Name":"a","Frames":["Assets/ok.png","Assets/gone.png"],"Fps":5}]},
          {"Name":"None","SpritePath":"Assets/gone.png"} ]}]}]}
        """);
        var sprites = EasyEngineBridge.ListSprites(eeproj);
        var half = Assert.Single(sprites);
        Assert.Equal("Half", half.Object);
        Assert.Single(half.Animations[0].Frames);
        Assert.Contains("Some pictures are missing and were skipped", half.Warnings);
    }

    [Fact]
    public void ToSprite_OneTagPerAnimation_FrameDurationFromFps()
    {
        WritePng("game/a.png", 8, 6, 0xFF0000FF);
        WritePng("game/b.png", 8, 6, 0xFF00FF00);
        WritePng("game/c.png", 4, 10, 0xFFFFFFFF);
        var eeproj = Project("""
        {"Scenes":[{"Layers":[{"Objects":[{"Name":"H","Animations":[
          {"Name":"walk","Frames":["a.png","b.png"],"Fps":8},
          {"Name":"jump","Frames":["c.png"],"Fps":10}]}]}]}]}
        """);
        var warnings = new List<string>();
        var sp = EasyEngineBridge.ToSprite(EasyEngineBridge.ListSprites(eeproj)[0], warnings);

        Assert.NotNull(sp);
        Assert.Equal((8, 10, 3), (sp!.Width, sp.Height, sp.Frames.Count)); // max width x max height
        Assert.Equal(new[] { 125, 125, 100 }, sp.Frames.Select(f => f.DurationMs));
        Assert.Equal(new[] { ("walk", 0, 1), ("jump", 2, 2) }, sp.Tags.Select(t => (t.Name, t.From, t.To)));
        var cel = sp.Cels[(sp.Layers[0], 2)];
        Assert.Equal((0, 0, 4, 10), (cel.X, cel.Y, cel.Image.Width, cel.Image.Height));
        Assert.Contains("Frames have different sizes", warnings);
    }

    [Fact]
    public void ToSprite_SamePicturesNoWarning_AndUnreadablePictureSkipped()
    {
        WritePng("game/a.png", 3, 3, 0xFF0000FF);
        File.WriteAllText(P("game", "broken.png"), "not a png");
        var eeproj = Project("""{"Scenes":[{"Layers":[{"Objects":[{"Name":"H","Animations":[{"Name":"w","Frames":["a.png","broken.png","a.png"]}]},{"Name":"B","SpritePath":"broken.png"}]}]}]}""");
        var listing = EasyEngineBridge.ListSprites(eeproj);
        var warnings = new List<string>();
        var sp = EasyEngineBridge.ToSprite(listing[0], warnings)!;
        Assert.Equal(2, sp.Frames.Count);
        Assert.DoesNotContain("Frames have different sizes", warnings);
        Assert.Contains("A picture could not be read and was skipped", warnings);
        Assert.Null(EasyEngineBridge.ToSprite(listing[1], new List<string>())); // nothing readable
    }

    [Fact]
    public void ListProjects_ReadsRecentJson_SkipsMissingPaths()
    {
        var real = Project("{}");
        var recent = P("recent.json");
        File.WriteAllText(recent, JsonSerializer.Serialize(new[]
        {
            new { Name = "Game", Path = real, LastOpened = "2026-10-01T10:00:00" },
            new { Name = "Gone", Path = P("gone", "x.eeproj"), LastOpened = "2026-10-02T10:00:00" },
        }));
        var list = EasyEngineBridge.ListProjects(recent);
        var one = Assert.Single(list);
        Assert.Equal(("Game", real), (one.Name, one.Path));
        Assert.Empty(EasyEngineBridge.ListProjects(P("none.json")));
        File.WriteAllText(recent, "garbage");
        Assert.Empty(EasyEngineBridge.ListProjects(recent));
    }

    private sealed class FakeRegistry : IInstallRegistry
    {
        public Dictionary<string, string> Values { get; } = new();
        public bool Throws { get; set; }
        public string? Read(string app) => Throws ? throw new InvalidOperationException("no registry") : Values.GetValueOrDefault(app);
        public void Write(string app, string path) => Values[app] = path;
    }

    [Fact]
    public void EngineNotInstalled_ReturnsNull_NoThrow()
    {
        var reg = new FakeRegistry();
        var disc = new AppDiscovery(reg, P("AppData"));
        Assert.Null(disc.FindEasyEngine());
        Assert.Null(disc.FindEasyPixel());
        reg.Values["EasyEngine"] = P("not", "there", "EasyEngine.exe"); // a stale value
        Assert.Null(disc.FindEasyEngine());
        reg.Throws = true; // no registry at all (another platform)
        Assert.Null(disc.FindEasyEngine());
        Assert.Null(disc.FindEasyPixel());
    }

    [Fact]
    public void Discovery_RegistryValue_ThenAppDataFallback()
    {
        var exe = P("bin", "EasyEngine.exe");
        Directory.CreateDirectory(P("bin"));
        File.WriteAllText(exe, "");
        var reg = new FakeRegistry();
        reg.Values["EasyEngine"] = exe;
        var disc = new AppDiscovery(reg, P("AppData"));
        Assert.Equal(new EngineInfo(exe, false), disc.FindEasyEngine());
        Directory.CreateDirectory(P("AppData", "EasyEngine"));
        File.WriteAllText(P("AppData", "EasyEngine", "recent.json"), "[]");
        Assert.Equal(new EngineInfo(exe, true), disc.FindEasyEngine());

        var fallback = new AppDiscovery(new FakeRegistry(), P("AppData"));
        Assert.Equal(new EngineInfo(null, true), fallback.FindEasyEngine());
        var pixel = new FakeRegistry();
        pixel.Values["EasyPixel"] = exe;
        Assert.Equal(exe, new AppDiscovery(pixel, P("AppData")).FindEasyPixel());
    }
}
