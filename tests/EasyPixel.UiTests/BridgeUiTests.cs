using System.Text.Json;
using Avalonia.Headless.XUnit;
using EasyPixel.Core.Bridge;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

public class BridgeUiTests
{
    private sealed class NoRegistry : IInstallRegistry
    {
        public string? Read(string app) => null;
        public void Write(string app, string path) { }
    }

    /// <summary>A fake "application data" folder (with or without EasyEngine in it) and a harness wired to it.</summary>
    private static (Harness H, string AppData, List<string> Opened) Setup(bool engineInstalled)
    {
        var appData = Path.GetDirectoryName(Harness.TempFile("x"))!;
        if (engineInstalled) Directory.CreateDirectory(Path.Combine(appData, "EasyEngine"));
        var h = new Harness(4, 4);
        var opened = new List<string>();
        h.Window.Discovery = new AppDiscovery(new NoRegistry(), appData);
        h.Window.ExportsRoot = Path.Combine(appData, "Exports");
        h.Window.EngineRecentFile = AppDiscovery.EngineRecentFile(appData);
        h.Window.OpenExternal = opened.Add;
        return (h, appData, opened);
    }

    [AvaloniaFact]
    public async Task Import_EngineNotInstalled_ExplainsAndOffersTheLink()
    {
        var (h, _, opened) = Setup(engineInstalled: false);
        using var _h = h;
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        await h.Window.ImportFromEngineAsync();
        Assert.Contains("github.com/tarl3s/EasyEngine", Assert.Single(h.Dialogs.Confirms));
        Assert.Equal(new[] { MainWindow.EngineUrl }, opened);
        Assert.Empty(h.Dialogs.ChoiceLists);

        h.Dialogs.ConfirmAnswers.Enqueue(false); // "Close": nothing opens
        await h.Window.ImportFromEngineAsync();
        Assert.Single(opened);
    }

    [AvaloniaFact]
    public async Task Import_FromRecentProject_OpensTheSpriteWithItsTags()
    {
        var (h, appData, _) = Setup(engineInstalled: true);
        using var _h = h;
        var game = Path.Combine(appData, "game");
        Directory.CreateDirectory(Path.Combine(game, "Assets"));
        foreach (var n in new[] { "a", "b" })
            File.WriteAllBytes(Path.Combine(game, "Assets", n + ".png"), Png.Encode(new Image(6, 5, 0xFF0000FF)));
        var eeproj = Path.Combine(game, "g.eeproj");
        File.WriteAllText(eeproj, """{"Scenes":[{"Name":"S","Layers":[{"Objects":[{"Name":"Hero","Animations":[{"Name":"walk","Frames":["Assets/a.png","Assets/b.png"],"Fps":5}]}]}]}]}""");
        File.WriteAllText(h.Window.EngineRecentFile!, JsonSerializer.Serialize(new[] { new { Name = "Game", Path = eeproj, LastOpened = "2026-10-01T10:00:00" } }));

        h.Dialogs.Choices.Enqueue(0); // the project
        h.Dialogs.Choices.Enqueue(0); // the sprite
        await h.Window.ImportFromEngineAsync();

        Assert.Contains("Game", h.Dialogs.ChoiceLists[0][0]);
        Assert.Equal("Browse…", h.Dialogs.ChoiceLists[0][^1]);
        Assert.StartsWith("S / Hero", h.Dialogs.ChoiceLists[1][0]);
        Assert.Equal((6, 5, 2), (h.Doc.Width, h.Doc.Height, h.Doc.Frames.Count));
        Assert.Equal(200, h.Doc.Frames[0].DurationMs);
        Assert.Equal("walk", Assert.Single(h.Doc.Tags).Name);
        Assert.Empty(h.Dialogs.Told);
    }

    [AvaloniaFact]
    public async Task Import_Cancelled_KeepsTheDocument()
    {
        var (h, _, _) = Setup(engineInstalled: true);
        using var _h = h;
        h.Drag((1, 1));
        h.Session.History.MarkSaved();
        var doc = h.Doc;
        await h.Window.ImportFromEngineAsync(); // no recent projects; the picker is cancelled
        Assert.Same(doc, h.Doc);
        Assert.Contains("Browse…", h.Dialogs.ChoiceLists[0]);
    }

    [AvaloniaFact]
    public async Task ExportToEngine_WritesTheHandoffAndCanOpenTheFolder()
    {
        var (h, appData, opened) = Setup(engineInstalled: false);
        using var _h = h;
        h.Doc.Frames.Add(new Frame());
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        await h.Window.ExportToEngineAsync();

        var root = Path.Combine(appData, "Exports");
        var info = Assert.Single(ExportHandoff.ListRecent(20, root));
        Assert.Equal(2, info.FrameCount);
        Assert.Contains("Import from EasyPixel", Assert.Single(h.Dialogs.Confirms));
        Assert.Equal(new[] { root }, opened);
    }
}
