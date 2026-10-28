using Avalonia.Headless.XUnit;

namespace EasyPixel.UiTests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindowConstructs()
    {
        var window = new MainWindow();
        window.Show();
        Assert.Equal("EasyPixel", window.Title);
        window.Close();
    }
}
