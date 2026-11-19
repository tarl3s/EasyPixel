using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

public class OpenManyTests
{
    private static string Write(string name, int w)
    {
        var path = Harness.TempFile(name);
        using var f = File.Create(path);
        new PngFormat().Write(new Sprite(w, w), f, new List<string>());
        return path;
    }

    [AvaloniaFact]
    public async Task OpenSeveralFiles_EachGetsItsOwnTab_NoQuestion()
    {
        using var h = new Harness();
        var a = Write("a.png", 8); var b = Write("b.png", 12); var c = Write("c.png", 20);
        h.Dialogs.OpenPathLists.Enqueue(new[] { a, b, c });
        await h.Window.OpenAsync();
        Assert.Equal(3, h.Window.Tabs.Count); // the untouched starting sprite gave its tab away
        Assert.Equal(new[] { a, b, c }, h.Window.Tabs.Select(t => t.Path));
        Assert.Empty(h.Dialogs.ChoiceLists); // no "open or place?" for several files
        Assert.Equal(20, h.Doc.Width);       // the last one is shown
    }

    [AvaloniaFact]
    public async Task OpenNothing_ChangesNothing()
    {
        using var h = new Harness();
        var before = h.Session;
        await h.Window.OpenAsync(); // cancelled: empty list
        Assert.Same(before, h.Session);
        Assert.Single(h.Window.Tabs);
    }
}
