using Avalonia;
using Avalonia.Headless;
using EasyPixel.UiTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace EasyPixel.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<EasyPixel.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
