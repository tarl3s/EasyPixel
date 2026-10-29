using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

public class AnimationFolderTests
{
    [AvaloniaFact]
    public async Task SavingAnAnimation_PutsTheEpxAndEveryFrameAsPng_InItsOwnFolder()
    {
        using var h = new Harness(8, 8);
        h.Doc.Frames.Add(new Frame());
        h.Doc.Frames.Add(new Frame());
        h.Doc.GetOrCreateCel(h.Doc.Layers[0], 1).Image.Set(1, 1, new Rgba(255, 0, 0, 255).Packed);
        var picked = Harness.TempFile("walk.epx");
        var dir = Path.Combine(Path.GetDirectoryName(picked)!, "walk");
        h.Dialogs.SavePaths.Enqueue(picked);
        Assert.True(await h.Window.SaveAsAsync());

        Assert.False(File.Exists(picked));
        Assert.Equal(Path.Combine(dir, "walk.epx"), h.Session.Path);
        Assert.True(File.Exists(Path.Combine(dir, "walk.epx")));
        foreach (var n in new[] { "walk_001.png", "walk_002.png", "walk_003.png" }) Assert.True(File.Exists(Path.Combine(dir, n)), n);
        Assert.Equal(new Rgba(255, 0, 0, 255).Packed, Png.Decode(File.ReadAllBytes(Path.Combine(dir, "walk_002.png"))).Get(1, 1));

        // one frame fewer: Save keeps the folder and its PNGs in step
        h.Doc.Frames.RemoveAt(2);
        Assert.True(await h.Window.SaveToAsync(h.Session.Path!));
        Assert.False(File.Exists(Path.Combine(dir, "walk_003.png")));
        Assert.True(File.Exists(Path.Combine(dir, "walk_002.png")));
    }

    [AvaloniaFact]
    public async Task SavingOneFrame_StaysAsChosen()
    {
        using var h = new Harness(8, 8);
        var picked = Harness.TempFile("tree.epx");
        h.Dialogs.SavePaths.Enqueue(picked);
        Assert.True(await h.Window.SaveAsAsync());
        Assert.True(File.Exists(picked));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(picked)!));
    }
}
