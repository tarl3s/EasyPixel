using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Dialogs;

/// <summary>The real dialogs: small modal windows over the main window, and the system's file pickers.</summary>
public sealed class AppDialogs : IDialogs
{
    private readonly Window _owner;

    public AppDialogs(Window owner) => _owner = owner;

    public async Task<SaveChoice> AskSaveChanges(string docName)
    {
        var w = DialogWindow.Message("EasyPixel", L.F("Save changes to {0}?", docName), L.T("Save"), L.T("Don't save"), L.T("Cancel"));
        await w.ShowDialog(_owner);
        return w.Result switch { 0 => SaveChoice.Save, 1 => SaveChoice.DontSave, _ => SaveChoice.Cancel };
    }

    public async Task<bool> Confirm(string message, string yes, string no)
    {
        var w = DialogWindow.Message("EasyPixel", message, yes, no);
        await w.ShowDialog(_owner);
        return w.Result == 0;
    }

    public async Task Tell(string message) => await DialogWindow.Message("EasyPixel", message, L.T("OK")).ShowDialog(_owner);

    public async Task<NewSpriteOptions?> AskNewSprite()
    {
        var w = new NewSpriteWindow();
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Options : null;
    }

    public async Task<ExportOptions?> AskExport()
    {
        var w = new ExportWindow();
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Options : null;
    }

    public async Task<string?> PickOpenFile(IReadOnlyList<FileType> types)
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("Open"),
            AllowMultiple = false,
            FileTypeFilter = Filters(types),
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveFile(string title, string suggestedName, IReadOnlyList<FileType> types)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = types.Count > 0 ? types[0].Extensions[0].TrimStart('.') : null,
            FileTypeChoices = Filters(types),
            ShowOverwritePrompt = true,
        });
        return file?.TryGetLocalPath();
    }

    public async Task<int?> AskNumber(string title, string label, int value, int min, int max)
    {
        var w = new NumberWindow(title, label, value, min, max);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Value : null;
    }

    public async Task<string?> AskText(string title, string label, string value)
    {
        var w = new TextWindow(title, label, value);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Value.Trim() : null;
    }

    public async Task<(int Width, int Height)?> AskSize(string title, int width, int height)
    {
        var w = SizeWindow.Simple(title, width, height, offerRatio: true);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Size : null;
    }

    public async Task<CanvasSizeOptions?> AskCanvasSize(int width, int height)
    {
        var w = new CanvasSizeWindow(width, height);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Options : null;
    }

    public async Task<int?> Choose(string title, string message, IReadOnlyList<string> options)
    {
        var w = new ChooseWindow(title, message, options);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Index : null;
    }

    public async Task<OnionOptions?> AskOnion(OnionOptions current)
    {
        var w = new OnionWindow(current);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Options : null;
    }

    public async Task<SettingsChoice?> AskSettings(SettingsChoice current)
    {
        var w = new SettingsWindow(current);
        await w.ShowDialog(_owner);
        return w.Result == 0 ? w.Choice : null;
    }

    public async Task ShowShortcuts(IReadOnlyList<(string Keys, string Action)> shortcuts) =>
        await new ShortcutsWindow(shortcuts).ShowDialog(_owner);

    private static List<FilePickerFileType> Filters(IReadOnlyList<FileType> types) =>
        types.Select(t => new FilePickerFileType(t.Name) { Patterns = t.Extensions.Select(e => "*" + e).ToList() }).ToList();
}

/// <summary>File → New: width, height (any size from 1) and colour mode.</summary>
internal sealed class NewSpriteWindow : DialogWindow
{
    private readonly NumericUpDown _w = Number(32), _h = Number(32);
    private readonly ComboBox _mode = new() { ItemsSource = new[] { L.T("RGBA"), L.T("Indexed (palette)") }, SelectedIndex = 0, MinWidth = 180 };

    public NewSpriteWindow() : base(L.T("New sprite"))
    {
        AddRow(L.T("Width"), _w);
        AddRow(L.T("Height"), _h);
        AddRow(L.T("Colour mode"), _mode);
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    public NewSpriteOptions Options =>
        new((int)(_w.Value ?? 1), (int)(_h.Value ?? 1), _mode.SelectedIndex == 1 ? ColorMode.Indexed : ColorMode.Rgba);

    internal static NumericUpDown Number(int value, int min = 1, int max = 65536) =>
        new() { Value = value, Minimum = min, Maximum = max, Increment = 1, FormatString = "0", MinWidth = 180 };
}

/// <summary>File → Export: a PNG of the current frame, an animated GIF, or a sprite sheet (PNG + JSON).</summary>
internal sealed class ExportWindow : DialogWindow
{
    private readonly ComboBox _kind = new() { ItemsSource = new[] { L.T("PNG image (current frame)"), L.T("Animated GIF"), L.T("Sprite sheet (PNG + JSON)") }, SelectedIndex = 0, MinWidth = 220 };
    private readonly ComboBox _layout = new() { ItemsSource = new[] { L.T("Horizontal"), L.T("Vertical"), L.T("Grid") }, SelectedIndex = 0, MinWidth = 220 };
    private readonly NumericUpDown _columns = NewSpriteWindow.Number(4, 1, 1024), _padding = NewSpriteWindow.Number(0, 0, 256);

    public ExportWindow() : base(L.T("Export"))
    {
        AddRow(L.T("Export as"), _kind);
        AddRow(L.T("Layout"), _layout);
        AddRow(L.T("Columns"), _columns);
        AddRow(L.T("Padding"), _padding);
        AddButton(L.T("Export…"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
        _kind.SelectionChanged += (_, _) => UpdateSheetFields();
        _layout.SelectionChanged += (_, _) => UpdateSheetFields();
        UpdateSheetFields();
    }

    private void UpdateSheetFields()
    {
        bool sheet = _kind.SelectedIndex == 2;
        _layout.IsEnabled = sheet;
        _padding.IsEnabled = sheet;
        _columns.IsEnabled = sheet && _layout.SelectedIndex == 2;
    }

    public ExportOptions Options => new(
        (ExportKind)Math.Max(0, _kind.SelectedIndex),
        (SheetKind)Math.Max(0, _layout.SelectedIndex),
        (int)(_columns.Value ?? 4),
        (int)(_padding.Value ?? 0));
}
