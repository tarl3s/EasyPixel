using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

/// <summary>A shown main window with scripted dialogs, and helpers to press on canvas pixels.</summary>
internal sealed class Harness : IDisposable
{
    public Harness(int width = 16, int height = 16, ColorMode mode = ColorMode.Rgba)
    {
        Window = new MainWindow { Width = 1000, Height = 700 };
        Dialogs = new FakeDialogs();
        Window.Dialogs = Dialogs;
        Window.Show();
        Window.SetSession(new EditorSession(new Sprite(width, height, mode)));
        Pump();
        Window.Canvas.FitToView();
    }

    public MainWindow Window { get; }
    public FakeDialogs Dialogs { get; }
    public EditorSession Session => Window.Session;
    public Sprite Doc => Session.Doc;

    public static void Pump() => Dispatcher.UIThread.RunJobs();

    /// <summary>The window point at the centre of canvas pixel (x, y).</summary>
    public Point At(int x, int y) => Window.Canvas.TranslatePoint(Window.Canvas.SpriteToScreen(x + 0.5, y + 0.5), Window)!.Value;

    public void Drag(MouseButton button, RawInputModifiers mods, params (int X, int Y)[] pixels)
    {
        Window.MouseDown(At(pixels[0].X, pixels[0].Y), button, mods);
        foreach (var p in pixels.Skip(1)) Window.MouseMove(At(p.X, p.Y), mods);
        Window.MouseUp(At(pixels[^1].X, pixels[^1].Y), button, mods);
        Pump();
    }

    public void Drag(params (int X, int Y)[] pixels) => Drag(MouseButton.Left, RawInputModifiers.None, pixels);

    public void Key(Key key, RawInputModifiers mods = RawInputModifiers.None)
    {
        Window.KeyPress(key, mods, PhysicalKey.None, null);
        Window.KeyRelease(key, mods, PhysicalKey.None, null);
        Pump();
    }

    /// <summary>The current layer's pixel at canvas (x, y) (0 where the cel doesn't reach).</summary>
    public uint Pixel(int x, int y)
    {
        if (!Doc.Cels.TryGetValue((Session.Layer, Session.Frame), out var cel)) return 0;
        return cel.Image.Get(x - cel.X, y - cel.Y);
    }

    public static string TempFile(string name)
    {
        var dir = Path.Combine(TestAppBuilder.TempRoot, Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name);
    }

    public void Dispose()
    {
        Session.DropFloating();
        Session.History.MarkSaved(); // no question when the test closes the window
        Window.Close();
        Pump();
    }
}
