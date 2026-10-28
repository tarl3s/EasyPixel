using Avalonia;
using Avalonia.Headless;
using EasyPixel.UiTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace EasyPixel.UiTests;

public static class TestAppBuilder
{
    /// <summary>This test run's own folder: settings and every file the tests write go here, removed at the end.</summary>
    public static string TempRoot { get; } = Path.Combine(Path.GetTempPath(), "EasyPixel.UiTests-" + Guid.NewGuid().ToString("N")[..8]);

    public static AppBuilder BuildAvaloniaApp()
    {
        Directory.CreateDirectory(TempRoot);
        EasyPixel.Settings.Folder = Path.Combine(TempRoot, "settings");
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(TempRoot, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        };
        // Real Skia drawing (not the headless stub), so the canvas's bitmaps and render path run as in the app.
        return AppBuilder.Configure<EasyPixel.App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
