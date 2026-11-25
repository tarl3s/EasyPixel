using Avalonia;
using Avalonia.Media;

namespace EasyPixel;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Files given to a second start (a double-click on an .epx) go to the EasyPixel already open, as tabs.
        var files = args.Where(a => !a.StartsWith('-')).ToList();
        if (files.Count > 0 && SingleInstance.TrySend(files)) return;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().With(Fonts()).LogToTrace();

    /// <summary>
    /// Where a character the UI font doesn't have comes from. On Windows the tool symbols (✎ ◪ ▦ …) come from Segoe UI
    /// Symbol; elsewhere the system's own fallback decides.
    /// </summary>
    public static FontManagerOptions Fonts()
    {
        if (!OperatingSystem.IsWindows()) return new FontManagerOptions();
        return new FontManagerOptions
        {
            DefaultFamilyName = "Segoe UI",
            FontFallbacks = new[] { new FontFallback { FontFamily = new FontFamily("Segoe UI Symbol") } },
        };
    }
}
