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
        Assert.NotNull(dir);
        var app = Path.Combine(dir!.FullName, "src", "EasyPixel");
        foreach (var file in Directory.EnumerateFiles(app, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            if (!file.EndsWith(".cs") && !file.EndsWith(".axaml")) continue;
            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, @"L\.[TF]\(""((?:[^""\\]|\\.)*)"""))
                Check(Regex.Unescape(m.Groups[1].Value));
            foreach (Match m in Regex.Matches(text, "loc:T '([^']*)'")) Check(m.Groups[1].Value);
        }
        var tools = new ToolController();
        foreach (ToolKind k in Enum.GetValues<ToolKind>()) Check(tools.Get(k).Name);
        foreach (var f in FormatRegistry.All) Check(f.Name);
        Assert.True(missing.Count == 0, "Missing from Lang/en.json (run tools/Update-Strings.sh): " + string.Join(" | ", missing));

        void Check(string s)
        {
            if (!en.ContainsKey(s) && !missing.Contains(s)) missing.Add(s);
        }
    }

    [AvaloniaFact]
    public void L_FallsBackToEnglish()
    {
        Assert.Equal("Save", L.T("Save"));
        Assert.Equal("not a key", L.T("not a key"));
        Assert.Equal("Saved a.epx", L.F("Saved {0}", "a.epx"));
    }
}
