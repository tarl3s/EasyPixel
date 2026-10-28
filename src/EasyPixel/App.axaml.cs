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
        ApplyTheme(Settings.Theme == "light");
    }

    private IResourceProvider? _palette;

    /// <summary>
    /// Puts the dark or light palette in place. At startup only in the app (open windows keep some colours they already
    /// took, so a change asks for a restart); the tests also switch it to measure both themes.
    /// </summary>
    internal void ApplyTheme(bool light)
    {
        RequestedThemeVariant = light ? ThemeVariant.Light : ThemeVariant.Dark;
        if (_palette != null) Resources.MergedDictionaries.Remove(_palette);
        _palette = (IResourceProvider)AvaloniaXamlLoader.Load(new Uri($"avares://EasyPixel/Theme/{(light ? "Light" : "Dark")}.axaml"));
        Resources.MergedDictionaries.Add(_palette);
    }

    /// <summary>Tells EasyEngine where EasyPixel lives (HKCU\Software\EasyPixel\InstallPath). A run through "dotnet" has no program of its own to point at.</summary>
    private static void RegisterInstall()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (exe != null && !Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                new WindowsInstallRegistry().Write("EasyPixel", exe);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException) { }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        CrashGuard.Install();
        RegisterInstall();
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
