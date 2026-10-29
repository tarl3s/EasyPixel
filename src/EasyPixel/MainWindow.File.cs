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
    private void ImportSheet_Click(object? sender, RoutedEventArgs e) => _ = ImportSheetAsync();

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
        var readable = FormatRegistry.All.Where(f => f.CanRead).ToList();
        var types = new List<FileType> { new(L.T("All supported files"), readable.SelectMany(f => f.Extensions).ToArray()) };
        types.AddRange(readable.Select(f => new FileType(L.T(f.Name), f.Extensions)));
        if (await Dialogs.PickOpenFile(types) is { } path) await OpenOrPlaceAsync(path);
    }

    /// <summary>
    /// File → Open and Recent: asks whether the file becomes the document (the current one is offered to be saved) or
    /// is placed in this canvas — its current picture pasted as floating pixels, e.g. a 32x32 drawing moved into a
    /// new 64x64 sprite.
    /// </summary>
    internal async Task OpenOrPlaceAsync(string path)
    {
        var choice = await Dialogs.Choose(L.T("Open"), L.F("How do you want to open {0}?", Path.GetFileName(path)),
            new[] { L.T("Open as its own sprite"), L.T("Place it in this canvas") });
        if (choice == 0) await OpenPathAsync(path);
        else if (choice == 1) await PlaceFileAsync(path);
    }

    /// <summary>Reads a file and pastes its first frame (all visible layers) into the current layer, floating.</summary>
    internal async Task<bool> PlaceFileAsync(string path)
    {
        if (await ReadAsync(path) is not { } read) return false;
        var src = read.Sprite;
        var flat = new Image(src.Width, src.Height);
        Compositor.Flatten(src, 0, flat, new PixelRect(0, 0, src.Width, src.Height));
        if (!PlaceFloating(flat, L.N("Place file"))) return false;
        ShowStatus(L.F("Placed {0}: move it, then press Enter", Path.GetFileName(path)));
        if (read.Warnings.Count > 0)
            await Dialogs.Tell(L.F("{0} was opened, with these notes:\n\n{1}", Path.GetFileName(path), Bullets(read.Warnings)));
        return true;
    }

    /// <summary>Opens a file (from Recent, the command line or a test), asking to save the current one first.</summary>
    public async Task<bool> OpenPathAsync(string path)
    {
        if (!await ConfirmDiscardAsync()) return false;
        return await LoadAsync(path);
    }

    private async Task<bool> LoadAsync(string path)
    {
        if (await ReadAsync(path) is not { } read) return false;
        var name = Path.GetFileName(path);
        SetSession(new EditorSession(read.Sprite, Path.GetFullPath(path)));
        RememberRecent(path);
        ShowStatus(L.F("Opened {0}", name));
        if (read.Warnings.Count > 0)
            await Dialogs.Tell(L.F("{0} was opened, with these notes:\n\n{1}", name, Bullets(read.Warnings)));
        return true;
    }

    /// <summary>Reads a sprite file; any problem is told to the person and gives null.</summary>
    private async Task<(Sprite Sprite, List<string> Warnings)?> ReadAsync(string path)
    {
        var name = Path.GetFileName(path);
        var format = FormatRegistry.ForPath(path);
        if (format == null || !format.CanRead)
        {
            await Dialogs.Tell(L.F("EasyPixel can't open this kind of file: {0}", name));
            return null;
        }
        var warnings = new List<string>();
        try
        {
            using var stream = File.OpenRead(path);
            return (format.Read(stream, warnings), warnings);
        }
        catch (Exception ex) // any reader error (broken or unexpected file) is reported, never a crash
        {
            await Dialogs.Tell(L.F("{0} could not be opened:\n{1}", name, ex.Message));
            return null;
        }
    }

    /// <summary>
    /// Saves to the document's file, or asks where when it has none or when that file is a picture (PNG, GIF) that
    /// can't keep the sprite as it is now (layers, frames, tags…). True when saved.
    /// </summary>
    internal async Task<bool> SaveAsync()
    {
        if (Session.Path is { } p && FormatRegistry.ForPath(p) is { CanWrite: true } f && !(f.Flattens && WouldLoseSomething(f)))
            return await SaveToAsync(p);
        return await SaveAsAsync();
    }

    private bool WouldLoseSomething(ISpriteFormat format)
    {
        Session.DropFloating();
        var warnings = new List<string>();
        try { format.Write(Session.Doc, Stream.Null, warnings); }
        catch (Exception) { return true; }
        FlatPicture.Warn(Session.Doc, warnings);
        return warnings.Count > 0;
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
    /// replaced (through a temp file, so a failure never leaves half a file). A picture format (PNG, GIF) that loses
    /// something works like an export: the document keeps its own file and stays unsaved.
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
        var warnings = new List<string>();
        void Write(Stream s, List<string> w)
        {
            format.Write(doc, s, w);
            if (format.Flattens) FlatPicture.Warn(doc, w);
        }
        if (!await WriteCheckedAsync(path, format.Name, Write, L.T("Save anyway"), warnings)) return false;
        if (format.Flattens && warnings.Count > 0)
        {
            ShowStatus(L.F("Exported {0}; the sprite itself is not saved", Path.GetFileName(path)));
            return false;
        }
        Session.Path = Path.GetFullPath(path);
        Session.History.MarkSaved();
        RememberRecent(path);
        UpdateTitle();
        ShowStatus(L.F("Saved {0}", Path.GetFileName(path)));
        return true;
    }

    /// <summary>Runs <paramref name="write"/> into memory, asks about its warnings, then writes the file atomically.</summary>
    private async Task<bool> WriteCheckedAsync(string path, string formatName, Action<Stream, List<string>> write, string goOn, List<string>? warnings = null)
    {
        warnings ??= new List<string>();
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

    /// <summary>
    /// File → Import sprite sheet: cuts a PNG into equal cells, left to right and top to bottom, and opens them as the
    /// frames of a new, unsaved sprite (the open one is not changed; it is offered to be saved first).
    /// </summary>
    internal async Task ImportSheetAsync(string? path = null)
    {
        if (!await ConfirmDiscardAsync()) return;
        path ??= await Dialogs.PickOpenFile(new[] { new FileType(L.T("PNG image"), new[] { ".png" }) });
        if (path == null) return;
        Image sheet;
        try { sheet = Png.Decode(await File.ReadAllBytesAsync(path)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            await Dialogs.Tell(L.F("{0} could not be opened:\n{1}", Path.GetFileName(path), ex.Message));
            return;
        }
        if (await Dialogs.AskSlice(Math.Min(sheet.Width, sheet.Height), sheet.Height) is not { } o) return;
        if (o.CellWidth < 1 || o.CellHeight < 1 || o.CellWidth > sheet.Width || o.CellHeight > sheet.Height)
        {
            await Dialogs.Tell(L.T("The cell is larger than the picture."));
            return;
        }
        var sprite = SpriteSheet.Slice(sheet, o.CellWidth, o.CellHeight, o.Padding);
        sprite.Palette.Colors.AddRange(Panels.PalettePanel.DefaultColors);
        SetSession(new EditorSession(sprite));
        ShowStatus(L.F("Imported {0} frames from {1}", sprite.Frames.Count, Path.GetFileName(path)));
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
            var item = L.Keep(new MenuItem { Header = new TextBlock { Text = p } }); // a file path, never translated
            var path = p;
            item.Click += async (_, _) =>
            {
                if (File.Exists(path)) { await OpenOrPlaceAsync(path); return; }
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
