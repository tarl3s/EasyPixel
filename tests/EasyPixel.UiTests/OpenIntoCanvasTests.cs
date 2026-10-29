using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

public class OpenIntoCanvasTests
{
    private static string Write32(uint color)
    {
        var small = new Sprite(32, 32, ColorMode.Rgba);
        small.GetOrCreateCel(small.Layers[0], 0).Image.Set(0, 0, color);
        var path = Harness.TempFile("small.png");
        using (var f = File.Create(path)) new PngFormat().Write(small, f, new List<string>());
        return path;
    }

    [AvaloniaFact]
    public async Task Open_PlaceInThisCanvas_PastesTheFile_Centred_AndUndoes()
    {
        var red = new Rgba(255, 0, 0, 255).Packed;
        var path = Write32(red);
        using var h = new Harness(64, 64);
        var doc = h.Doc;
        h.Dialogs.OpenPaths.Enqueue(path);
        h.Dialogs.Choices.Enqueue(1);
        await h.Window.OpenAsync();

        Assert.Same(doc, h.Doc); // same canvas, not a new document
        Assert.Equal(2, h.Dialogs.ChoiceLists[0].Count);
        Assert.NotNull(h.Session.Float);
        h.Key(Key.Enter);
        Assert.Equal(red, h.Pixel(16, 16)); // 32x32 centred in 64x64
        Assert.Equal((64, 64), (h.Doc.Width, h.Doc.Height));
        Assert.Null(h.Session.Path);

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(0u, h.Pixel(16, 16));
    }

    [AvaloniaFact]
    public async Task Open_AsItsOwnSprite_ReplacesTheDocument()
    {
        var path = Write32(new Rgba(0, 0, 255, 255).Packed);
        using var h = new Harness(64, 64);
        h.Dialogs.OpenPaths.Enqueue(path);
        h.Dialogs.Choices.Enqueue(0);
        await h.Window.OpenAsync();
        Assert.Equal((32, 32), (h.Doc.Width, h.Doc.Height));
        Assert.Equal(path, h.Session.Path);
    }

    [AvaloniaFact]
    public async Task Open_Cancelled_ChangesNothing()
    {
        var path = Write32(new Rgba(0, 0, 255, 255).Packed);
        using var h = new Harness(64, 64);
        var doc = h.Doc;
        h.Dialogs.OpenPaths.Enqueue(path);
        await h.Window.OpenAsync(); // no answer = cancelled
        Assert.Same(doc, h.Doc);
        Assert.Null(h.Session.Float);
    }
}
