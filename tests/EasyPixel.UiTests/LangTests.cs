using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Tools;

namespace EasyPixel.UiTests;

public class LangTests
{
    private static Dictionary<string, string> EnTable()
    {
        var json = L.ReadEmbedded("en");
        Assert.NotNull(json);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json!)!;
    }

    /// <summary>Every text the sources pass to L.T / L.F or loc:T, and every tool and format name, is in Lang/en.json (run tools/Update-Strings.sh).</summary>
    [AvaloniaFact]
    public void Lang_EnJsonHasEveryText()
    {
        var en = EnTable();
        var missing = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "EasyPixel.sln"))) dir = dir.Parent;
        Assert.True(dir != null,
            $"EasyPixel.sln not found above {AppContext.BaseDirectory}: this test reads the app's sources, run it from the repository (dotnet test).");
        var app = Path.Combine(dir!.FullName, "src", "EasyPixel");
        foreach (var file in Directory.EnumerateFiles(app, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            if (!file.EndsWith(".cs") && !file.EndsWith(".axaml")) continue;
            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, @"L\.[TFN]\(""((?:[^""\\]|\\.)*)"""))
                Check(Regex.Unescape(m.Groups[1].Value));
            foreach (Match m in Regex.Matches(text, "loc:T '([^']*)'")) Check(m.Groups[1].Value);
        }
        var tools = new ToolController();
        foreach (ToolKind k in Enum.GetValues<ToolKind>()) Check(tools.Get(k).Name);
        foreach (var f in FormatRegistry.All) Check(f.Name);
        foreach (var (_, action) in MainWindow.Shortcuts) Check(action);
        // What the file formats warn about is shown through L.T too (MainWindow.Bullets).
        var formats = Path.Combine(dir.FullName, "src", "EasyPixel.Core", "Formats");
        foreach (var file in Directory.EnumerateFiles(formats, "*.cs"))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"(?:warnings\.Add\(|Warning = )""([^""]+)"""))
                Check(m.Groups[1].Value);
        // Undo step names of the Core edits ("Undo {0}").
        foreach (var file in Directory.EnumerateFiles(Path.Combine(dir.FullName, "src", "EasyPixel.Core", "History"), "*.cs"))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"public string Name => ""([^""]+)"""))
                Check(m.Groups[1].Value);
        Assert.True(missing.Count == 0, "Missing from Lang/en.json (run tools/Update-Strings.sh): " + string.Join(" | ", missing));

        void Check(string s)
        {
            if (!en.ContainsKey(s) && !missing.Contains(s)) missing.Add(s);
        }
    }

    public static IEnumerable<object[]> OtherLanguages => L.Languages.Where(l => l.Code != "en").Select(l => new object[] { l.Code });

    /// <summary>Each language translates at least 97% of the English texts, keeps every {0} and has no stale keys.</summary>
    [Theory]
    [MemberData(nameof(OtherLanguages))]
    public void Language_IsComplete_AndKeepsPlaceholders(string code)
    {
        var en = EnTable().Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => kv.Value);
        var json = L.ReadEmbedded(code);
        Assert.True(json != null, $"Lang/{code}.json is not embedded");
        var table = JsonSerializer.Deserialize<Dictionary<string, string>>(json!)!
            .Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => kv.Value);
        int translated = en.Keys.Count(k => table.TryGetValue(k, out var t) && !string.IsNullOrWhiteSpace(t));
        Assert.True(translated >= en.Count * 0.97, $"{code}: {translated} of {en.Count} texts translated");
        var stale = table.Keys.Where(k => !en.ContainsKey(k)).ToList();
        Assert.True(stale.Count == 0, $"{code} has texts English no longer has: " + string.Join(" | ", stale));
        var broken = new List<string>();
        foreach (var (key, value) in table)
        {
            var want = Regex.Matches(key, @"\{\d+\}").Select(m => m.Value).OrderBy(v => v);
            var got = Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).OrderBy(v => v);
            if (!want.SequenceEqual(got)) { broken.Add(key); continue; }
            try { _ = string.Format(value, "a", "b", "c"); }
            catch (FormatException) { broken.Add(key); }
        }
        Assert.True(broken.Count == 0, $"{code} breaks the placeholders of: " + string.Join(" | ", broken));
    }

    [AvaloniaFact]
    public void Language_LoadsAndFallsBack()
    {
        try
        {
            L.Load("pt-BR");
            Assert.Equal("pt-BR", L.Language);
            Assert.Equal("Salvar", L.T("Save"));
            L.Load("xx");
            Assert.Equal("en", L.Language);
            Assert.Equal("Save", L.T("Save"));
        }
        finally { L.Load("en"); }
    }

    [AvaloniaFact]
    public void L_FallsBackToEnglish()
    {
        Assert.Equal("Save", L.T("Save"));
        Assert.Equal("not a key", L.T("not a key"));
        Assert.Equal("Saved a.epx", L.F("Saved {0}", "a.epx"));
    }
}
