using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace EasyPixel;

public partial class App : Application
{
    /// <summary>The settings loaded at startup; saved when they change.</summary>
    public static Settings Settings { get; private set; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Settings = Settings.Load();
        L.Load(L.Resolve(Settings.Language));
        bool light = Settings.Theme == "light";
        RequestedThemeVariant = light ? ThemeVariant.Light : ThemeVariant.Dark;
        var palette = (IResourceProvider)AvaloniaXamlLoader.Load(new Uri($"avares://EasyPixel/Theme/{(light ? "Light" : "Dark")}.axaml"));
        Resources.MergedDictionaries.Add(palette);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var file = desktop.Args?.FirstOrDefault(a => !a.StartsWith('-'));
            if (file != null) window.Opened += async (_, _) => await window.OpenPathAsync(file);
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
