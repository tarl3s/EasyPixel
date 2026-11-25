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
            if (desktop.Args is { } cli && cli.Contains("--ai-sheet"))
            {
                // Dev tool: no window; run the quality contact sheet and exit.
                _ = Task.Run(async () =>
                {
                    int code;
                    try { code = await EasyPixel.Ai.AiSheet.RunCommandAsync(cli); }
                    catch (Exception ex) { Console.Error.WriteLine(ex.Message); code = 1; }
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => desktop.Shutdown(code));
                });
                base.OnFrameworkInitializationCompleted();
                return;
            }
            var window = new MainWindow();
            var files = desktop.Args?.Where(a => !a.StartsWith('-')).ToList() ?? new();
            if (files.Count > 0) window.Opened += async (_, _) => { foreach (var f in files) await window.OpenPathAsync(f); };
            // later starts with files hand them over here (Program.Main): open them as tabs and come to the front
            var stopListening = new CancellationTokenSource();
            window.Closed += (_, _) => stopListening.Cancel();
            SingleInstance.Listen(paths => Avalonia.Threading.Dispatcher.UIThread.Post(async () => await window.OpenHandedFilesAsync(paths)), stopListening.Token);
            window.Opened += (_, _) => _ = window.CheckForUpdatesAsync();
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
