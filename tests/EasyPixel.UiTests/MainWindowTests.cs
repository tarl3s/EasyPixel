using Avalonia.Headless.XUnit;

namespace EasyPixel.UiTests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindowConstructs()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            Assert.Equal("Untitled - EasyPixel", window.Title);
        }
        finally { window.Close(); }
    }
}
