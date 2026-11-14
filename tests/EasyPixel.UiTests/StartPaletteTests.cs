using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Panels;

namespace EasyPixel.UiTests;

public class StartPaletteTests
{
    [AvaloniaFact]
    public async Task LoadedPalette_IsRemembered_ForNewSprites_AndCanGoBackToDefault()
    {
        using var h = new Harness();
        var mine = new List<Rgba> { new(10, 20, 30, 255), new(200, 100, 50, 255), new(1, 2, 3, 255) };
        var path = Harness.TempFile("mine.gpl");
        PaletteFormats.Write(path, mine);
        await h.Window.PaletteView.LoadAsync(path);

        Assert.Equal(mine, PalettePanel.StartColors);
        Assert.Equal(3, App.Settings.StartPalette.Count);
        var saved = EasyPixel.Settings.Load(); // survives a restart
        Assert.Equal(App.Settings.StartPalette, saved.StartPalette);

        h.Dialogs.NewSprites.Enqueue(new NewSpriteOptions(8, 8, ColorMode.Rgba));
        await h.Window.NewAsync();
        Assert.Equal(mine, h.Doc.Palette.Colors);

        App.Settings.StartPalette.Clear();
        Assert.Equal(PalettePanel.DefaultColors, PalettePanel.StartColors);
    }
}
