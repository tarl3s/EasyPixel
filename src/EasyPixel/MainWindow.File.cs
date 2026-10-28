using Avalonia.Controls;
using Avalonia.Interactivity;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;
using EasyPixel.Dialogs;

namespace EasyPixel;

// File menu: new, open, recent, save, save as, export, and asking to save before the document goes away.
public partial class MainWindow
{
    /// <summary>Canvases above this size ask before they are made (memory and speed).</summary>
    public const int LargeCanvas = 4096;

    private void New_Click(object? sender, RoutedEventArgs e) => _ = NewAsync();
    private void Open_Click(object? sender, RoutedEventArgs e) => _ = OpenAsync();
    private void Save_Click(object? sender, RoutedEventArgs e) => _ = SaveAsync();
    private void SaveAs_Click(object? sender, RoutedEventArgs e) => _ = SaveAsAsync();
    private void Export_Click(object? sender, RoutedEventArgs e) => _ = ExportAsync();
    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Before the document is replaced or the window closes: when it has unsaved changes, asks Save / Don't save /
    /// Cancel. True when it is fine to go on (saved, or the person chose not to save).
    /// </summary>
    internal async Task<bool> ConfirmDiscardAsync()
    {
        Session.DropFloating();
        if (!Session.History.IsDirty) return true;
        return await Dialogs.AskSaveChanges(Session.DisplayName) switch
        {
            SaveChoice.Save => await SaveAsync(),
            SaveChoice.DontSave => true,
            _ => false,
        };
    }

    internal async Task NewAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        if (await Dialogs.AskNewSprite() is not { } o) return;
        if (o.Width < 1 || o.Height < 1) return;
        if ((o.Width > LargeCanvas || o.Height > LargeCanvas)
            && !await Dialogs.Confirm(L.F("A {0} x {1} canvas is very large: it may be slow or run out of memory. Create it anyway?", o.Width, o.Height),
                L.T("Create"), L.T("Cancel")))
            return;
        Sprite sprite;
        try { sprite = NewDocument(o.Width, o.Height, o.Mode); }
        catch (Exception ex) when (ex is OutOfMemoryException or OverflowException)
        {
            await Dialogs.Tell(L.T("There isn't enough memory for a canvas that big."));
            return;
        }
        SetSession(new EditorSession(sprite));
        ShowStatus(L.F("New {0} x {1} sprite", o.Width, o.Height));
    }

    internal async Task OpenAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        var readable = FormatRegistry.All.Where(f => f.CanRead).ToList();
        var types = new List<FileType> { new(L.T("All supported files"), readable.SelectMany(f => f.Extensions).ToArray()) };
        types.AddRange(readable.Select(f => new FileType(L.T(f.Name), f.Extensions)));
        if (await Dialogs.PickOpenFile(types) is { } path) await LoadAsync(path);
    }

    /// <summary>Opens a file (from Recent, the command line or a test), asking to save the current one first.</summary>
    public async Task<bool> OpenPathAsync(string path)
    {
        if (!await ConfirmDiscardAsync()) return false;
        return await LoadAsync(path);
    }

    private async Task<bool> LoadAsync(string path)
    {
        var name = Path.GetFileName(path);
        var format = FormatRegistry.ForPath(path);
        if (format == null || !format.CanRead)
        {
            await Dialogs.Tell(L.F("EasyPixel can't open this kind of file: {0}", name));
            return false;
        }
        var warnings = new List<string>();
        Sprite sprite;
        try
        {
            using var stream = File.OpenRead(path);
            sprite = format.Read(stream, warnings);
        }
        catch (Exception ex) // any reader error (broken or unexpected file) is reported, never a crash
        {
            await Dialogs.Tell(L.F("{0} could not be opened:\n{1}", name, ex.Message));
            return false;
        }
        SetSession(new EditorSession(sprite, Path.GetFullPath(path)));
        RememberRecent(path);
        ShowStatus(L.F("Opened {0}", name));
        if (warnings.Count > 0)
            await Dialogs.Tell(L.F("{0} was opened, with these notes:\n\n{1}", name, Bullets(warnings)));
        return true;
    }

    /// <summary>Saves to the document's file, or asks where when it has none. True when saved.</summary>
    internal async Task<bool> SaveAsync()
    {
        if (Session.Path is { } p && FormatRegistry.ForPath(p) is { CanWrite: true }) return await SaveToAsync(p);
        return await SaveAsAsync();
    }

    internal async Task<bool> SaveAsAsync()
    {
        var writable = FormatRegistry.All.Where(f => f.CanWrite).ToList(); // .epx is first in the registry
        var types = writable.Select(f => new FileType(L.T(f.Name), f.Extensions)).ToList();
        var baseName = Session.Path != null ? Path.GetFileNameWithoutExtension(Session.Path) : L.T("Untitled");
        var path = await Dialogs.PickSaveFile(L.T("Save as"), baseName + ".epx", types);
        return path != null && await SaveToAsync(path);
    }

    /// <summary>
    /// Writes the document in the format of <paramref name="path"/>'s extension. The file is first written in memory:
    /// when the format can't keep everything, the person sees what would be lost and decides; only then is the file
    /// replaced (through a temp file, so a failure never leaves half a file).
    /// </summary>
    internal async Task<bool> SaveToAsync(string path)
    {
        Session.DropFloating();
        var format = FormatRegistry.ForPath(path);
        if (format == null || !format.CanWrite)
        {
            await Dialogs.Tell(L.F("EasyPixel can't save this kind of file: {0}", Path.GetFileName(path)));
            return false;
        }
        var doc = Session.Doc;
        if (!await WriteCheckedAsync(path, format.Name, (s, w) => format.Write(doc, s, w), L.T("Save anyway"))) return false;
        Session.Path = Path.GetFullPath(path);
        Session.History.MarkSaved();
        RememberRecent(path);
        UpdateTitle();
        ShowStatus(L.F("Saved {0}", Path.GetFileName(path)));
        return true;
    }

    /// <summary>Runs <paramref name="write"/> into memory, asks about its warnings, then writes the file atomically.</summary>
    private async Task<bool> WriteCheckedAsync(string path, string formatName, Action<Stream, List<string>> write, string goOn)
    {
        var warnings = new List<string>();
        var ms = new MemoryStream();
        try { write(ms, warnings); }
        catch (Exception ex)
        {
            await Dialogs.Tell(L.F("The file could not be written:\n{0}", ex.Message));
            return false;
        }
        if (warnings.Count > 0
            && !await Dialogs.Confirm(L.F("{0} can't keep everything in this sprite:\n\n{1}\n\nWrite the file anyway?", L.T(formatName), Bullets(warnings)), goOn, L.T("Cancel")))
            return false;
        try
        {
            WriteAtomically(path, ms);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            await Dialogs.Tell(L.F("The file could not be written:\n{0}", ex.Message));
            return false;
        }
        return true;
    }

    /// <summary>Writes next to the target, then moves over it: the old file stays whole until the new one is complete.</summary>
    internal static void WriteAtomically(string path, MemoryStream data)
    {
        var full = Path.GetFullPath(path);
        var tmp = Path.Combine(Path.GetDirectoryName(full)!, "." + Path.GetFileName(full) + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp");
        try
        {
            using (var f = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write))
            {
                data.Position = 0;
                data.CopyTo(f);
                f.Flush(true);
            }
            File.Move(tmp, full, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    internal async Task ExportAsync()
    {
        Session.DropFloating();
        if (await Dialogs.AskExport() is not { } o) return;
        var doc = Session.Doc;
        var baseName = Session.Path != null ? Path.GetFileNameWithoutExtension(Session.Path) : L.T("Untitled");
        var (ext, typeName) = o.Kind == ExportKind.Gif ? (".gif", "GIF animation") : (".png", "PNG image");
        var path = await Dialogs.PickSaveFile(L.T("Export"), baseName + ext, new[] { new FileType(L.T(typeName), new[] { ext }) });
        if (path == null) return;
        bool ok;
        switch (o.Kind)
        {
            case ExportKind.Gif:
                ok = await WriteCheckedAsync(path, typeName, (s, w) => new GifFormat().Write(doc, s, w), L.T("Export anyway"));
                break;
            case ExportKind.Sheet:
                {
                    var layout = o.Layout == SheetKind.Grid ? SheetLayout.Grid(o.Columns) : new SheetLayout(o.Layout);
                    var jsonPath = Path.ChangeExtension(path, ".json");
                    // The picker only asked about the PNG; the JSON next to it may be someone's file too.
                    if (File.Exists(jsonPath)
                        && !await Dialogs.Confirm(L.F("{0} already exists. Replace it?", Path.GetFileName(jsonPath)), L.T("Replace"), L.T("Cancel")))
                        return;
                    var (sheet, json) = SpriteSheet.Export(doc, layout, o.Padding);
                    ok = await WriteCheckedAsync(path, typeName, (s, _) => s.Write(Png.Encode(sheet)), L.T("Export anyway"));
                    if (ok)
                        ok = await WriteCheckedAsync(jsonPath, "JSON", (s, _) => s.Write(System.Text.Encoding.UTF8.GetBytes(json)), L.T("Export anyway"));
                    break;
                }
            default:
                {
                    var flat = new Image(doc.Width, doc.Height);
                    Compositor.Flatten(doc, Session.Frame, flat, Session.CanvasRect);
                    ok = await WriteCheckedAsync(path, typeName, (s, _) => s.Write(Png.Encode(flat)), L.T("Export anyway"));
                    break;
                }
        }
        if (ok) ShowStatus(L.F("Exported {0}", Path.GetFileName(path)));
    }

    private void RememberRecent(string path)
    {
        App.Settings.AddRecent(path);
        App.Settings.Save();
        BuildRecentMenu();
    }

    private void BuildRecentMenu()
    {
        var items = new List<Control>();
        foreach (var p in App.Settings.Recent)
        {
            // A TextBlock, not a string: a string header would turn "_" in the path into an access key.
            var item = new MenuItem { Header = new TextBlock { Text = p } };
            var path = p;
            item.Click += async (_, _) =>
            {
                if (File.Exists(path)) { await OpenPathAsync(path); return; }
                App.Settings.Recent.Remove(path);
                App.Settings.Save();
                BuildRecentMenu();
                await Dialogs.Tell(L.F("{0} isn't there any more.", path));
            };
            items.Add(item);
        }
        if (items.Count == 0) items.Add(new MenuItem { Header = L.T("No recent files"), IsEnabled = false });
        RecentMenu.ItemsSource = items;
    }

    private static string Bullets(IEnumerable<string> lines) => string.Join("\n", lines.Select(l => "• " + L.T(l)));
}
