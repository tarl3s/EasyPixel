using Avalonia.Interactivity;
using EasyPixel.Dialogs;

namespace EasyPixel;

// View menu (zoom, grids, tiled preview, onion skin, panels, theme), Edit → Settings and the Help menu.
public partial class MainWindow
{
    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => Canvas.ZoomStep(1);
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => Canvas.ZoomStep(-1);
    private void Fit_Click(object? sender, RoutedEventArgs e) => Canvas.FitToView();

    private void ZoomPreset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.MenuItem { Tag: string pct } && double.TryParse(pct, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var p))
            Canvas.ZoomAt(p / 100, new Avalonia.Point(Canvas.Bounds.Width / 2, Canvas.Bounds.Height / 2));
    }

    private void Grid_Click(object? sender, RoutedEventArgs e)
    {
        Canvas.ShowGrid = GridItem.IsChecked;
        App.Settings.ShowGrid = Canvas.ShowGrid;
        App.Settings.Save();
    }

    private void TileGrid_Click(object? sender, RoutedEventArgs e)
    {
        Canvas.ShowTileGrid = TileGridItem.IsChecked;
        App.Settings.ShowTileGrid = Canvas.ShowTileGrid;
        App.Settings.Save();
    }

    private void GridSettings_Click(object? sender, RoutedEventArgs e) => _ = GridSettingsAsync();

    /// <summary>The tile grid's cell size; turns the tile grid on.</summary>
    internal async Task GridSettingsAsync()
    {
        var s = App.Settings;
        if (await Dialogs.AskSize(L.T("Grid settings"), s.GridWidth, s.GridHeight) is not { } size) return;
        s.GridWidth = Math.Clamp(size.Width, 1, 4096);
        s.GridHeight = Math.Clamp(size.Height, 1, 4096);
        s.ShowTileGrid = true;
        s.Save();
        ApplyViewSettings();
    }

    private void ApplyViewSettings()
    {
        var s = App.Settings;
        Canvas.ShowGrid = s.ShowGrid;
        GridItem.IsChecked = s.ShowGrid;
        Canvas.TileGrid = (Math.Max(1, s.GridWidth), Math.Max(1, s.GridHeight));
        Canvas.ShowTileGrid = s.ShowTileGrid;
        TileGridItem.IsChecked = s.ShowTileGrid;
        ApplyOnionSettings();
    }

    private void Tiled_Click(object? sender, RoutedEventArgs e) => Canvas.Tiled = TiledItem.IsChecked;
    private void Onion_Click(object? sender, RoutedEventArgs e) => SetOnion(OnionItem.IsChecked);
    private void Panels_Click(object? sender, RoutedEventArgs e) => TogglePanels();

    /// <summary>Tab: hides the tool strip, options, panels and timeline to give the canvas the whole window (again shows them).</summary>
    internal void TogglePanels()
    {
        bool show = !RightPanel.IsVisible;
        RightPanel.IsVisible = show;
        TimelineBar.IsVisible = show;
        ToolBar.IsVisible = show;
        OptionsBorder.IsVisible = show;
        PanelsItem.IsChecked = show;
    }

    private void ThemeDark_Click(object? sender, RoutedEventArgs e) => _ = SetThemeAsync("dark");
    private void ThemeLight_Click(object? sender, RoutedEventArgs e) => _ = SetThemeAsync("light");

    private async Task SetThemeAsync(string theme)
    {
        UpdateThemeMenu(theme);
        if (App.Settings.Theme == theme) return;
        App.Settings.Theme = theme;
        App.Settings.Save();
        await Dialogs.Tell(L.T("The new theme is used the next time EasyPixel starts."));
    }

    private void UpdateThemeMenu(string theme)
    {
        ThemeDarkItem.IsChecked = theme != "light";
        ThemeLightItem.IsChecked = theme == "light";
    }

    private void Settings_Click(object? sender, RoutedEventArgs e) => _ = SettingsAsync();

    /// <summary>Edit → Settings: language and theme; both apply after a restart.</summary>
    internal async Task SettingsAsync()
    {
        var s = App.Settings;
        if (await Dialogs.AskSettings(new SettingsChoice(s.Language, s.Theme)) is not { } c) return;
        if (c.Language == s.Language && c.Theme == s.Theme) return;
        s.Language = c.Language;
        s.Theme = c.Theme;
        s.Save();
        UpdateThemeMenu(c.Theme);
        await Dialogs.Tell(L.T("Changes apply the next time EasyPixel starts."));
    }

    // ---- Help ----

    private void Shortcuts_Click(object? sender, RoutedEventArgs e) => _ = ShowShortcutsAsync();

    internal Task ShowShortcutsAsync() => Dialogs.ShowShortcuts(Shortcuts.Select(s => (s.Keys, L.T(s.Action))).ToList());

    private void About_Click(object? sender, RoutedEventArgs e) =>
        _ = Dialogs.Tell(L.F("EasyPixel {0}\nA free pixel-art editor (MIT licence).\ngithub.com/tarl3s/EasyPixel",
            typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? ""));
}
