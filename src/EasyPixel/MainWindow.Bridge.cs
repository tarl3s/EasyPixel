using System.Diagnostics;
using Avalonia.Interactivity;
using EasyPixel.Core.Bridge;
using EasyPixel.Dialogs;

namespace EasyPixel;

// File → Import sprite from EasyEngine… / Export to EasyEngine: the bridge to the sibling game engine. EasyPixel only
// reads an EasyEngine project; it hands animations over in a folder EasyEngine imports from.
public partial class MainWindow
{
    public const string EngineUrl = "https://github.com/tarl3s/EasyEngine";

    /// <summary>Finds EasyEngine (tests give it a fake registry and folder).</summary>
    internal AppDiscovery Discovery { get; set; } = new(new WindowsInstallRegistry());

    /// <summary>Where exports are written; null = the real <c>%LOCALAPPDATA%\EasyPixel\Exports</c> (tests use their own folder).</summary>
    internal string? ExportsRoot { get; set; }

    /// <summary>EasyEngine's recent-projects file; null = the real one.</summary>
    internal string? EngineRecentFile { get; set; }

    /// <summary>Opens a folder or a web page with the system (tests record the calls instead).</summary>
    internal Action<string> OpenExternal { get; set; } = target =>
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException) { }
    };

    private void ImportEngine_Click(object? sender, RoutedEventArgs e) => _ = ImportFromEngineAsync();
    private void ExportEngine_Click(object? sender, RoutedEventArgs e) => _ = ExportToEngineAsync();

    internal async Task ImportFromEngineAsync()
    {
        if (Discovery.FindEasyEngine() == null)
        {
            if (await Dialogs.Confirm(L.T("EasyEngine isn't installed on this computer, so there is nothing to import yet.\n\nEasyEngine is a free, easy 2D game engine: github.com/tarl3s/EasyEngine"),
                    L.T("Open the page"), L.T("Close")))
                OpenExternal(EngineUrl);
            return;
        }
        if (!await ConfirmDiscardAsync()) return;
        if (await PickEngineProjectAsync() is not { } eeproj) return;

        IReadOnlyList<EngineSprite> sprites;
        try { sprites = EasyEngineBridge.ListSprites(eeproj); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            await Dialogs.Tell(L.F("{0} could not be opened:\n{1}", Path.GetFileName(eeproj), ex.Message));
            return;
        }
        if (sprites.Count == 0)
        {
            await Dialogs.Tell(L.T("This project has no sprites with pictures to import."));
            return;
        }
        var lines = sprites.Select(SpriteLine).ToList();
        if (await Dialogs.Choose(L.T("Import from EasyEngine"), L.T("Pick the sprite to edit."), lines) is not { } i) return;

        var warnings = new List<string>(sprites[i].Warnings);
        var sprite = EasyEngineBridge.ToSprite(sprites[i], warnings);
        if (sprite == null)
        {
            await Dialogs.Tell(L.F("No picture of {0} could be read.", sprites[i].Object));
            return;
        }
        SetSession(new EditorSession(sprite));
        ShowStatus(L.F("Imported {0}", sprites[i].Object));
        if (warnings.Count > 0)
            await Dialogs.Tell(L.F("{0} was imported, with these notes:\n\n{1}", sprites[i].Object, Bullets(warnings)));
    }

    /// <summary>One list line: "Scene / Object (animation, animation)"; prefabs have no scene.</summary>
    private static string SpriteLine(EngineSprite s)
    {
        var names = s.Animations.Where(a => a.Name != null).Select(a => a.Name!).ToList();
        var where = s.Scene.Length > 0 ? s.Scene : L.T("Prefab");
        return where + " / " + s.Object + (names.Count > 0 ? "  (" + string.Join(", ", names) + ")" : "");
    }

    /// <summary>The project to import from: one of EasyEngine's recent ones, or a file picked with Browse. Null when cancelled.</summary>
    private async Task<string?> PickEngineProjectAsync()
    {
        var projects = EasyEngineBridge.ListProjects(EngineRecentFile);
        var options = projects.Select(p => p.Name + "  —  " + p.Path).ToList();
        options.Add(L.T("Browse…"));
        var message = projects.Count > 0 ? L.T("Pick an EasyEngine project.") : L.T("EasyEngine has no recent projects to show. Browse for a project file.");
        if (await Dialogs.Choose(L.T("Import from EasyEngine"), message, options) is not { } choice) return null;
        if (choice < projects.Count) return projects[choice].Path;
        return await Dialogs.PickOpenFile(new[] { new FileType(L.T("EasyEngine project"), new[] { ".eeproj" }) });
    }

    internal async Task ExportToEngineAsync()
    {
        Session.DropFloating();
        var name = Session.Path != null ? Path.GetFileNameWithoutExtension(Session.Path) : L.T("Untitled");
        IReadOnlyList<string> folders;
        try { folders = ExportHandoff.WriteAll(Session.Doc, name, ExportsRoot, Session.Path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await Dialogs.Tell(L.F("The file could not be written:\n{0}", ex.Message));
            return;
        }
        ShowStatus(L.F("Exported {0}", name));
        var names = string.Join(", ", folders.Select(f => Path.GetFileName(f)));
        if (await Dialogs.Confirm(L.F("Exported: {0}\n\nNow click Import from EasyPixel in EasyEngine.", names), L.T("Open folder"), L.T("Close")))
            OpenExternal(Path.GetDirectoryName(folders[0])!);
    }
}
