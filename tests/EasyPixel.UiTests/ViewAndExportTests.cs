using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;

namespace EasyPixel.UiTests;

public class ViewAndExportTests
{
    [AvaloniaFact]
    public void OnionSkin_ShowsPreviousFrameUnderneath()
    {
        using var h = new Harness(8, 8);
        h.Drag((3, 3));
        h.Doc.Frames.Add(new Frame());
        h.Session.Frame = 1;
        h.Window.Canvas.RefreshAll();
        Assert.Equal(0u, h.Window.Canvas.ShownPixel(3, 3));

        h.Window.Canvas.ShowOnion = true;
        uint ghost = h.Window.Canvas.ShownPixel(3, 3);
        Assert.InRange(ghost >> 24, 1u, 254u); // a see-through copy of frame 0's pixel
        h.Window.Canvas.ShowOnion = false;
        Assert.Equal(0u, h.Window.Canvas.ShownPixel(3, 3));
    }

    [AvaloniaFact]
    public async Task Export_Png_Gif_Sheet()
    {
        using var h = new Harness(4, 4);
        h.Drag((1, 1));
        h.Doc.Frames.Add(new Frame());

        var png = Harness.TempFile("frame.png");
        h.Dialogs.Exports.Enqueue(new ExportOptions(ExportKind.Png));
        h.Dialogs.SavePaths.Enqueue(png);
        await h.Window.ExportAsync();
        var img = Png.Decode(File.ReadAllBytes(png));
        Assert.Equal(new Rgba(0, 0, 0, 255).Packed, img.Get(1, 1));

        var gif = Harness.TempFile("anim.gif");
        h.Dialogs.Exports.Enqueue(new ExportOptions(ExportKind.Gif));
        h.Dialogs.SavePaths.Enqueue(gif);
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        await h.Window.ExportAsync();
        using (var f = File.OpenRead(gif)) Assert.Equal(2, new GifFormat().Read(f, new List<string>()).Frames.Count);

        var sheet = Harness.TempFile("sheet.png");
        h.Dialogs.Exports.Enqueue(new ExportOptions(ExportKind.Sheet, SheetKind.Horizontal, 1, 2));
        h.Dialogs.SavePaths.Enqueue(sheet);
        await h.Window.ExportAsync();
        var s = Png.Decode(File.ReadAllBytes(sheet));
        Assert.Equal((10, 4), (s.Width, s.Height));
        var jsonPath = Path.ChangeExtension(sheet, ".json");
        Assert.True(File.Exists(jsonPath));

        // Exporting again over an existing JSON asks first; "no" writes nothing.
        File.WriteAllText(jsonPath, "mine");
        File.WriteAllText(sheet, "old");
        h.Dialogs.Exports.Enqueue(new ExportOptions(ExportKind.Sheet));
        h.Dialogs.SavePaths.Enqueue(sheet);
        h.Dialogs.ConfirmAnswers.Clear(); // (the GIF step may not have used its answer)
        h.Dialogs.ConfirmAnswers.Enqueue(false);
        await h.Window.ExportAsync();
        Assert.Contains("sheet.json", h.Dialogs.Confirms[^1]);
        Assert.Equal("mine", File.ReadAllText(jsonPath));
        Assert.Equal("old", File.ReadAllText(sheet));
        Assert.True(h.Session.History.IsDirty); // exporting doesn't count as saving
    }
}
