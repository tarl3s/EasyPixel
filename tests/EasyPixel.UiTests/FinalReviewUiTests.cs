using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Dialogs;
using EasyPixel.Tools;
using Image = EasyPixel.Core.Model.Image;

namespace EasyPixel.UiTests;

/// <summary>Fixes from the final v0.1.0 review, driven through the window.</summary>
public class FinalReviewUiTests
{
    private static Harness IndexedWith(int opaqueColours)
    {
        var h = new Harness(16, 16, ColorMode.Indexed);
        for (int i = 0; i < opaqueColours; i++) h.Doc.Palette.Colors.Add(new Rgba((byte)(20 + 10 * i), 40, 40, 255));
        h.Window.RefreshPanels();
        return h;
    }

    private static void AssertIndicesValid(Sprite doc)
    {
        foreach (var cel in doc.Cels.Values.Distinct())
            Assert.All(cel.Image.Pixels, p => Assert.True(p < (uint)doc.Palette.Colors.Count, $"index {p} past a {doc.Palette.Colors.Count}-colour palette"));
        Assert.True(doc.Palette.TransparentIndex >= 0);
    }

    [AvaloniaFact]
    public void Indexed_NewColourStroke_IsUndoneWithItsPaletteEntry_RedoAcrossRemoveColourKeepsPixels()
    {
        using var h = IndexedWith(16); // 17 entries
        var pal = h.Window.PaletteView;
        pal.Select(5);
        pal.RemoveSelected(); // 16 entries
        Assert.Equal(16, h.Doc.Palette.Colors.Count);

        var fresh = new Rgba(1, 200, 3, 255);
        h.Window.Tools.SetPrimary(fresh);
        h.Drag((3, 3));
        Assert.Equal(17, h.Doc.Palette.Colors.Count);
        Assert.Equal(16u, h.Pixel(3, 3));

        h.Window.Undo();
        Assert.Equal(16, h.Doc.Palette.Colors.Count); // the new entry went with the stroke
        h.Window.Undo();
        Assert.Equal(17, h.Doc.Palette.Colors.Count);
        h.Window.Redo();
        h.Window.Redo();
        Assert.Equal(17, h.Doc.Palette.Colors.Count);
        Assert.Equal(fresh, h.Doc.Palette.Colors[(int)h.Pixel(3, 3)]);
        AssertIndicesValid(h.Doc);
    }

    [AvaloniaFact]
    public async Task Indexed_PasteOfNewColours_UndoRemovesThem_CancelRemovesThem()
    {
        using var h = IndexedWith(2);
        int before = h.Doc.Palette.Colors.Count;
        var pic = new Image(2, 1);
        pic.Pixels[0] = new Rgba(250, 1, 1, 255).Packed;
        pic.Pixels[1] = new Rgba(1, 250, 1, 255).Packed;
        MainWindow.InternalClipboard = pic;

        await h.Window.PasteAsync();
        Assert.Equal(before + 2, h.Doc.Palette.Colors.Count);
        h.Session.CancelFloating(); // Esc: the paste never happened
        Assert.Equal(before, h.Doc.Palette.Colors.Count);

        await h.Window.PasteAsync();
        h.Session.DropFloating();
        Assert.Equal(before + 2, h.Doc.Palette.Colors.Count);
        AssertIndicesValid(h.Doc);
        h.Window.Undo();
        Assert.Equal(before, h.Doc.Palette.Colors.Count);
        AssertIndicesValid(h.Doc);
        h.Window.Redo();
        Assert.Equal(before + 2, h.Doc.Palette.Colors.Count);
        AssertIndicesValid(h.Doc);
        MainWindow.InternalClipboard = null;
    }

    [AvaloniaFact]
    public void Indexed_RemovingTheTransparentColour_IsRefusedWithAMessage()
    {
        using var h = IndexedWith(3);
        var pal = h.Window.PaletteView;
        pal.Select(h.Doc.Palette.TransparentIndex);
        pal.RemoveSelected();
        Assert.Equal(4, h.Doc.Palette.Colors.Count);
        Assert.Equal(0, h.Doc.Palette.TransparentIndex);
        Assert.False(h.Session.History.CanUndo);
        Assert.Contains("transparent", h.Window.StatusText.Text);
    }

    [AvaloniaFact]
    public void Indexed_WithoutTransparentEntry_EraserAddsOne_AsPartOfTheStroke()
    {
        using var h = IndexedWith(2);
        var doc = h.Doc;
        doc.Palette.Colors[0] = new Rgba(5, 5, 5, 255); // the old transparent entry became a colour
        doc.Palette.TransparentIndex = -1;
        Array.Fill(h.Session.CelForDrawing().Image.Pixels, 1u);
        h.Window.Tools.Current = ToolKind.Eraser;
        h.Drag((4, 4));
        Assert.Equal(4, doc.Palette.Colors.Count);
        Assert.Equal(3, doc.Palette.TransparentIndex);
        Assert.Equal(3u, h.Pixel(4, 4)); // not colour 0
        Assert.Equal(1u, h.Pixel(5, 5));
        h.Window.Undo();
        Assert.Equal(3, doc.Palette.Colors.Count);
        Assert.Equal(-1, doc.Palette.TransparentIndex);
        Assert.Equal(1u, h.Pixel(4, 4));
    }

    [AvaloniaFact]
    public async Task SaveAs_LayeredSpriteToPng_ListsWhatIsLost_AndKeepsTheDocumentUnsaved()
    {
        using var h = new Harness(8, 8);
        h.Drag((1, 1));
        h.Doc.Layers.Add(new Layer { Name = "Top", Visible = false });
        h.Doc.Tags.Add(new Tag { Name = "t" });
        var png = Harness.TempFile("flat.png");
        h.Dialogs.SavePaths.Enqueue(png);
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        Assert.False(await h.Window.SaveAsAsync());
        Assert.True(File.Exists(png));
        var asked = Assert.Single(h.Dialogs.Confirms);
        Assert.Contains("merged", asked);
        Assert.Contains("Hidden layers", asked);
        Assert.Contains("Tags", asked);
        Assert.Null(h.Session.Path);
        Assert.True(h.Session.History.IsDirty);

        // A plain one-layer picture saved as PNG is a real save.
        using var plain = new Harness(8, 8);
        plain.Drag((1, 1));
        var png2 = Harness.TempFile("plain.png");
        plain.Dialogs.SavePaths.Enqueue(png2);
        Assert.True(await plain.Window.SaveAsAsync());
        Assert.Equal(Path.GetFullPath(png2), plain.Session.Path);
        Assert.False(plain.Session.History.IsDirty);

        // Ctrl+S on that PNG once the sprite has layers asks for a new file instead of flattening it.
        plain.Doc.Layers.Add(new Layer());
        plain.Drag((2, 2));
        var epx = Harness.TempFile("layered.epx");
        plain.Dialogs.SavePaths.Enqueue(epx);
        Assert.True(await plain.Window.SaveAsync());
        Assert.Equal(Path.GetFullPath(epx), plain.Session.Path);
    }

    [AvaloniaFact]
    public async Task ImportSpriteSheet_CutsCellsIntoFrames_AsANewDocument()
    {
        using var h = new Harness();
        var sheet = new Image(26, 8);
        for (int c = 0; c < 3; c++) sheet.Set(c * 9 + 1, 1, new Rgba(255, (byte)(c * 50), 0, 255).Packed); // 8x8 cells, 1 px gap
        var path = Harness.TempFile("sheet.png");
        File.WriteAllBytes(path, Png.Encode(sheet));
        h.Dialogs.OpenPaths.Enqueue(path);
        h.Dialogs.Slices.Enqueue(new SliceOptions(8, 8, 1));
        await h.Window.ImportSheetAsync();
        Assert.Equal((8, 8), (h.Doc.Width, h.Doc.Height));
        Assert.Equal(3, h.Doc.Frames.Count);
        Assert.Equal(new Rgba(255, 100, 0, 255).Packed, h.Doc.Cels[(h.Doc.Layers[0], 2)].Image.Get(1, 1));
        Assert.Null(h.Session.Path);

        h.Dialogs.OpenPaths.Enqueue(path);
        h.Dialogs.Slices.Enqueue(new SliceOptions(40, 8, 0));
        await h.Window.ImportSheetAsync();
        Assert.Contains(h.Dialogs.Told, t => t.Contains("larger"));
    }

    [AvaloniaFact]
    public void FlipSelection_WithShiftH_MirrorsTheSelectedPixels_AndUndoes()
    {
        using var h = new Harness(16, 16);
        var red = new Rgba(255, 0, 0, 255);
        h.Window.Tools.SetPrimary(red);
        h.Drag((2, 3));
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(2, 2, 4, 4));
        h.Key(Key.H, RawInputModifiers.Shift);
        Assert.NotNull(h.Session.Float);
        h.Key(Key.Enter); // drop
        Assert.Equal(0u, h.Pixel(2, 3));
        Assert.Equal(red.Packed, h.Pixel(5, 3));
        Assert.True(h.Session.Sel.Contains(5, 3));
        h.Window.Undo();
        Assert.Equal(red.Packed, h.Pixel(2, 3));
        Assert.Equal(0u, h.Pixel(5, 3));
    }

    [AvaloniaFact]
    public void RotateAndFlip_WithNothingSelected_ChangeTheWholeLayer_InOneStep()
    {
        using var h = new Harness(8, 8);
        var red = new Rgba(255, 0, 0, 255);
        h.Window.Tools.SetPrimary(red);
        h.Drag((0, 0));
        int steps = h.Session.History.Count;
        h.Window.RotateSelection(clockwise: true);
        Assert.Null(h.Session.Float);
        Assert.True(h.Session.Sel.IsEmpty);
        Assert.Equal(steps + 1, h.Session.History.Count);
        Assert.Equal(red.Packed, h.Pixel(7, 0));
        h.Key(Key.V, RawInputModifiers.Shift); // flip vertically
        Assert.Equal(red.Packed, h.Pixel(7, 7));
        h.Window.Undo();
        h.Window.Undo();
        Assert.Equal(red.Packed, h.Pixel(0, 0));
    }

    [AvaloniaFact]
    public async Task ScaleSelection_DoublesTheFloatingPixels_AndTheOutlineFollows()
    {
        using var h = new Harness(16, 16);
        h.Window.Tools.SetPrimary(new Rgba(0, 0, 255, 255));
        h.Drag((4, 4), (5, 4));
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(4, 4, 2, 2));
        h.Dialogs.Numbers.Enqueue(200);
        await h.Window.ScaleSelectionAsync();
        var fs = Assert.IsType<FloatingState>(h.Session.Float);
        Assert.Equal((4, 4), (fs.Pixels.Pixels.Width, fs.Pixels.Pixels.Height));
        Assert.Equal((3, 3), (fs.Pixels.X, fs.Pixels.Y)); // around the same centre
        Assert.True(fs.SelectionAtLift.Contains(6, 6));
        h.Session.DropFloating();
        Assert.Equal(new Rgba(0, 0, 255, 255).Packed, h.Pixel(6, 3));
        Assert.True(h.Session.Sel.Contains(6, 6));
    }

    [AvaloniaFact]
    public void CrashGuard_RepeatingError_IsLoggedAndToldOnce_WithOneRecoveryCopyPerMinute()
    {
        using var h = new Harness();
        var folder = Path.Combine(TestAppBuilder.TempRoot, "crash-repeat");
        var now = new DateTime(2026, 10, 28, 12, 0, 0);
        CrashGuard.Reset();
        CrashGuard.Clock = () => now;
        CrashGuard.Folder = folder;
        CrashGuard.Window = h.Window;
        CrashGuard.Install();
        try
        {
            h.Drag((1, 1)); // unsaved work
            void Throw(string message)
            {
                var frame = new Avalonia.Threading.DispatcherFrame();
                for (int i = 0; i < 3; i++)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => throw new InvalidOperationException(message));
                Avalonia.Threading.Dispatcher.UIThread.Post(() => frame.Continue = false);
                Avalonia.Threading.Dispatcher.UIThread.PushFrame(frame);
                Harness.Pump();
            }
            Throw("tick failed");
            var log = File.ReadAllText(CrashGuard.LogPath);
            Assert.Equal(1, log.Split("tick failed").Length - 1);
            Assert.Single(Directory.GetFiles(folder, "easypixel_recovery_*.epx"));
            Assert.Single(h.Dialogs.Told);

            now = now.AddSeconds(20); // another error soon after: logged, told, but no second copy yet
            Throw("draw failed");
            Assert.Single(Directory.GetFiles(folder, "easypixel_recovery_*.epx"));
            Assert.Contains("repeated 2 more times", File.ReadAllText(CrashGuard.LogPath));
            Assert.Equal(2, h.Dialogs.Told.Count);

            now = now.AddMinutes(2);
            Throw("tick failed");
            Assert.Equal(2, Directory.GetFiles(folder, "easypixel_recovery_*.epx").Length);
        }
        finally
        {
            CrashGuard.UninstallUiHandler();
            CrashGuard.Window = null;
            CrashGuard.Clock = () => DateTime.Now;
            CrashGuard.Reset();
        }
    }
}
