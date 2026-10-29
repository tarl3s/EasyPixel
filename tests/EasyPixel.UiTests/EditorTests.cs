using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Tools;
using Image = EasyPixel.Core.Model.Image;
using PixelRect = EasyPixel.Core.Model.PixelRect;

namespace EasyPixel.UiTests;

public class EditorTests
{
    private static readonly uint Black = new Rgba(0, 0, 0, 255).Packed;

    [AvaloniaFact]
    public async Task NewSprite_DrawPencil_PixelSet()
    {
        using var h = new Harness();
        h.Dialogs.NewSprites.Enqueue(new NewSpriteOptions(20, 10, ColorMode.Rgba));
        await h.Window.NewAsync();
        Assert.Equal((20, 10), (h.Doc.Width, h.Doc.Height));
        h.Window.Canvas.FitToView();

        h.Drag((3, 4), (6, 4));

        for (int x = 3; x <= 6; x++) Assert.Equal(Black, h.Pixel(x, 4));
        Assert.Equal(0u, h.Pixel(7, 4));
        Assert.Equal(0u, h.Pixel(3, 5));
        Assert.True(h.Session.History.IsDirty);
        Assert.EndsWith("* - EasyPixel", h.Window.Title);
    }

    [AvaloniaFact]
    public void UndoShortcut_RevertsStroke()
    {
        using var h = new Harness();
        h.Drag((1, 1), (4, 1));
        Assert.Equal(Black, h.Pixel(2, 1));

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(0u, h.Pixel(2, 1));
        Assert.False(h.Session.History.IsDirty);

        h.Key(Key.Y, RawInputModifiers.Control);
        Assert.Equal(Black, h.Pixel(2, 1));
        h.Key(Key.Z, RawInputModifiers.Control);
        h.Key(Key.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal(Black, h.Pixel(2, 1));
    }

    [AvaloniaFact]
    public async Task CloseWithChanges_AsksToSave()
    {
        var h = new Harness();
        h.Drag((2, 2));

        h.Dialogs.SaveChoices.Enqueue(SaveChoice.Cancel);
        h.Window.Close();
        Harness.Pump();
        Assert.Equal(1, h.Dialogs.SaveQuestions);
        Assert.True(h.Window.IsVisible); // Cancel keeps the window and the drawing

        // Save: asks where, writes, then closes.
        var path = Harness.TempFile("closing.epx");
        h.Dialogs.SaveChoices.Enqueue(SaveChoice.Save);
        h.Dialogs.SavePaths.Enqueue(path);
        h.Window.Close();
        Harness.Pump();
        Assert.Equal(2, h.Dialogs.SaveQuestions);
        Assert.True(File.Exists(path));
        Assert.False(h.Window.IsVisible);

        // New and Open ask too; Don't save goes on without saving.
        using var h2 = new Harness();
        h2.Drag((1, 1));
        h2.Dialogs.SaveChoices.Enqueue(SaveChoice.DontSave);
        h2.Dialogs.NewSprites.Enqueue(new NewSpriteOptions(8, 8, ColorMode.Rgba));
        await h2.Window.NewAsync();
        Assert.Equal(1, h2.Dialogs.SaveQuestions);
        Assert.Equal(8, h2.Doc.Width);

        h2.Drag((1, 1));
        h2.Dialogs.SaveChoices.Enqueue(SaveChoice.Cancel);
        Assert.False(await h2.Window.OpenPathAsync(path));
        Assert.Equal(2, h2.Dialogs.SaveQuestions);
        Assert.Equal(Black, h2.Pixel(1, 1)); // still the same document
    }

    [AvaloniaFact]
    public async Task OpenAseprite_ThenSave_KeepsFile()
    {
        var sprite = new Sprite(6, 5);
        sprite.Layers.Add(new Layer { Name = "Top", Opacity = 200 });
        sprite.Frames.Add(new Frame { DurationMs = 250 });
        sprite.Tags.Add(new Tag { Name = "walk", From = 0, To = 1 });
        sprite.GetOrCreateCel(sprite.Layers[0], 0).Image.Set(1, 1, 0xFF0000FF);
        sprite.GetOrCreateCel(sprite.Layers[1], 1).Image.Set(4, 3, 0xFF00FF00);
        var path = Harness.TempFile("walk.aseprite");
        using (var f = File.Create(path)) new Core.Formats.AsepriteFormat().Write(sprite, f, new List<string>());
        var original = File.ReadAllBytes(path);

        using var h = new Harness();
        Assert.True(await h.Window.OpenPathAsync(path));
        Assert.Empty(h.Dialogs.Told); // no load warnings for a file EasyPixel wrote
        Assert.Equal(path, h.Session.Path);
        Assert.Equal("walk.aseprite - EasyPixel", h.Window.Title);

        Assert.True(await h.Window.SaveAsync());
        Assert.Empty(h.Dialogs.Confirms); // nothing lost: no question
        Assert.False(h.Session.History.IsDirty);

        Sprite back;
        using (var f = File.OpenRead(path)) back = new Core.Formats.AsepriteFormat().Read(f, new List<string>());
        Assert.Equal(2, back.Layers.Count);
        Assert.Equal(2, back.Frames.Count);
        Assert.Equal(250, back.Frames[1].DurationMs);
        Assert.Equal("walk", Assert.Single(back.Tags).Name);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!)); // no temp file left behind
        Assert.Contains(path, App.Settings.Recent);
    }

    [AvaloniaFact]
    public async Task SaveWithWarnings_AsksFirst()
    {
        using var h = new Harness();
        h.Doc.Frames.Add(new Frame()); // PNG keeps only the first frame
        h.Drag((1, 1));
        var path = Harness.TempFile("two-frames.png");
        File.WriteAllText(path, "old");

        h.Dialogs.ConfirmAnswers.Enqueue(false);
        Assert.False(await h.Window.SaveToAsync(path));
        Assert.Single(h.Dialogs.Confirms);
        Assert.Contains("Only the first frame was saved", h.Dialogs.Confirms[0]);
        Assert.Equal("old", File.ReadAllText(path)); // nothing written when the person says no
        Assert.True(h.Session.History.IsDirty);

        h.Dialogs.ConfirmAnswers.Enqueue(true);
        Assert.False(await h.Window.SaveToAsync(path)); // written, but it is an export: the sprite itself isn't saved
        Assert.Equal(0x89, File.ReadAllBytes(path)[0]); // a PNG now
        Assert.True(h.Session.History.IsDirty);
        Assert.Null(h.Session.Path);
    }

    [AvaloniaFact]
    public async Task NewSprite_Above4096_WarnsFirst()
    {
        using var h = new Harness();
        h.Dialogs.NewSprites.Enqueue(new NewSpriteOptions(5000, 2, ColorMode.Rgba));
        h.Dialogs.ConfirmAnswers.Enqueue(false);
        await h.Window.NewAsync();
        Assert.Single(h.Dialogs.Confirms);
        Assert.Equal(16, h.Doc.Width);

        h.Dialogs.NewSprites.Enqueue(new NewSpriteOptions(5000, 2, ColorMode.Indexed));
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        await h.Window.NewAsync();
        Assert.Equal((5000, 2, ColorMode.Indexed), (h.Doc.Width, h.Doc.Height, h.Doc.Mode));
    }

    [AvaloniaFact]
    public void Zoom_6400_NoException()
    {
        using var h = new Harness(64, 64);
        var canvas = h.Window.Canvas;
        canvas.ZoomAt(64, new Point(100, 100));
        Assert.Equal(6400, canvas.ZoomPercent, 3);
        h.Window.Canvas.ShowGrid = true;
        h.Drag((30, 30), (31, 31));
        Assert.NotNull(h.Window.CaptureRenderedFrame());

        for (int i = 0; i < 40; i++) h.Window.MouseWheel(h.At(31, 31), new Vector(0, 1), RawInputModifiers.None);
        Assert.Equal(6400, canvas.ZoomPercent, 3); // stops at the top
        for (int i = 0; i < 40; i++) h.Window.MouseWheel(new Point(300, 300), new Vector(0, -1), RawInputModifiers.Control);
        Assert.Equal(1, canvas.ZoomPercent, 3);
        Assert.NotNull(h.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void ZoomAroundCursor_KeepsPixelUnderPointer()
    {
        using var h = new Harness(64, 64);
        var canvas = h.Window.Canvas;
        var p = canvas.SpriteToScreen(20.5, 10.5);
        canvas.ZoomStep(3, p);
        Assert.Equal((20, 10), canvas.ScreenToSprite(p));
    }

    [AvaloniaFact]
    public void Bucket_FillsInsideOnly_AndUndoes()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.Current = ToolKind.Rectangle;
        h.Drag((1, 1), (6, 6));
        h.Window.Tools.Current = ToolKind.Bucket;
        h.Window.Tools.SetPrimary(new Rgba(255, 0, 0, 255));
        h.Drag((3, 3));

        uint red = new Rgba(255, 0, 0, 255).Packed;
        Assert.Equal(red, h.Pixel(3, 3));
        Assert.Equal(red, h.Pixel(5, 5));
        Assert.Equal(Black, h.Pixel(1, 1));
        Assert.Equal(0u, h.Pixel(8, 8));

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(0u, h.Pixel(3, 3));
        Assert.Equal(Black, h.Pixel(1, 1));
    }

    [AvaloniaFact]
    public void SelectionMove_DragsPixels_DropUndoes()
    {
        using var h = new Harness(10, 10);
        h.Drag((2, 2));
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((1, 1), (3, 3));
        Assert.Equal(new PixelRect(1, 1, 3, 3), h.Session.Sel.Bounds);

        h.Window.Tools.Current = ToolKind.Move;
        h.Drag((2, 2), (4, 3), (5, 5));
        Assert.NotNull(h.Session.Float);
        Assert.NotNull(h.Window.CaptureRenderedFrame()); // floating pixels + marching ants drawn
        h.Key(Key.Enter);
        Assert.Null(h.Session.Float);
        Assert.Equal(Black, h.Pixel(5, 5));
        Assert.Equal(0u, h.Pixel(2, 2));
        Assert.Equal(new PixelRect(4, 4, 3, 3), h.Session.Sel.Bounds); // the selection followed

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(Black, h.Pixel(2, 2));
        Assert.Equal(0u, h.Pixel(5, 5));
    }

    [AvaloniaFact]
    public void MoveOutsideCanvas_GrowsCel_UndoRestores()
    {
        using var h = new Harness(8, 8);
        h.Drag((1, 1));
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((0, 0), (2, 2));
        h.Window.Tools.Current = ToolKind.Move;
        for (int i = 0; i < 3; i++) h.Key(Key.Left);
        h.Key(Key.Enter);
        var cel = h.Doc.Cels[(h.Session.Layer, 0)];
        Assert.Equal(-2, cel.X); // the pixel at -2 is kept outside the canvas
        Assert.Equal(Black, h.Pixel(-2, 1));

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal((0, 0, 8), (cel.X, cel.Y, cel.Image.Width));
        Assert.Equal(Black, h.Pixel(1, 1));
    }

    [AvaloniaFact]
    public void SelectionTools_ShiftAdds_AltSubtracts()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((0, 0), (3, 3));
        h.Drag(MouseButton.Left, RawInputModifiers.Shift, (6, 6), (7, 7));
        Assert.True(h.Session.Sel.Contains(1, 1) && h.Session.Sel.Contains(7, 7));
        h.Drag(MouseButton.Left, RawInputModifiers.Alt, (0, 0), (1, 1));
        Assert.False(h.Session.Sel.Contains(1, 1));
        Assert.True(h.Session.Sel.Contains(2, 2));

        h.Key(Key.D, RawInputModifiers.Control);
        Assert.True(h.Session.Sel.IsEmpty);
        h.Key(Key.A, RawInputModifiers.Control);
        Assert.Equal(new PixelRect(0, 0, 10, 10), h.Session.Sel.Bounds);
    }

    [AvaloniaFact]
    public void SelectionClipsPencil_AndDeleteClears()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((2, 0), (5, 9));
        h.Window.Tools.Current = ToolKind.Pencil;
        h.Drag((0, 4), (9, 4));
        Assert.Equal(0u, h.Pixel(1, 4));
        Assert.Equal(Black, h.Pixel(3, 4));
        h.Key(Key.Delete);
        Assert.Equal(0u, h.Pixel(3, 4));
        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(Black, h.Pixel(3, 4));
    }

    [AvaloniaFact]
    public async Task CopyPaste_FloatsThenDrops()
    {
        using var h = new Harness(10, 10);
        h.Drag((1, 1), (2, 1));
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((1, 1), (2, 1));
        await h.Window.CopyAsync();
        await h.Window.PasteAsync();
        Assert.Equal(ToolKind.Move, h.Window.Tools.Current);
        Assert.NotNull(h.Session.Float);
        h.Drag((1, 1), (1, 5));
        h.Key(Key.Enter);
        Assert.Equal(Black, h.Pixel(1, 5));
        Assert.Equal(Black, h.Pixel(2, 5));
        Assert.Equal(Black, h.Pixel(1, 1)); // the original stays

        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(0u, h.Pixel(1, 5));
    }

    [AvaloniaFact]
    public void AltClick_PicksColour_WithPaintTool()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.SetPrimary(new Rgba(10, 20, 30, 255));
        h.Drag((4, 4));
        h.Window.Tools.SetPrimary(new Rgba(0, 0, 0, 255));
        h.Drag(MouseButton.Left, RawInputModifiers.Alt, (4, 4));
        Assert.Equal(new Rgba(10, 20, 30, 255), h.Window.Tools.Primary);
        Assert.Equal(ToolKind.Pencil, h.Window.Tools.Current);
    }

    [AvaloniaFact]
    public void PixelPerfect_DropsLCorners()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.SetPixelPerfect(true);
        h.Drag((1, 1), (2, 1), (2, 2), (3, 2), (3, 3));
        Assert.Equal(0u, h.Pixel(2, 1));
        Assert.Equal(0u, h.Pixel(3, 2));
        Assert.Equal(Black, h.Pixel(1, 1));
        Assert.Equal(Black, h.Pixel(2, 2));
        Assert.Equal(Black, h.Pixel(3, 3));
    }

    [AvaloniaFact]
    public void Mirror_PaintsBothHalves_AndRightButtonUsesSecondary()
    {
        using var h = new Harness(10, 10);
        h.Window.Tools.SetMirror(true, false);
        h.Drag(MouseButton.Right, RawInputModifiers.None, (1, 2));
        uint white = new Rgba(255, 255, 255, 255).Packed;
        Assert.Equal(white, h.Pixel(1, 2));
        Assert.Equal(white, h.Pixel(8, 2));
    }

    [AvaloniaFact]
    public void Indexed_PencilAddsPaletteEntry_EraserClears()
    {
        using var h = new Harness(8, 8, ColorMode.Indexed);
        h.Window.Tools.SetPrimary(new Rgba(200, 10, 10, 255));
        h.Drag((2, 2));
        uint idx = h.Pixel(2, 2);
        Assert.Equal(new Rgba(200, 10, 10, 255), h.Doc.Palette.Colors[(int)idx]);
        h.Window.Tools.Current = ToolKind.Eraser;
        h.Drag((2, 2));
        Assert.Equal((uint)h.Doc.Palette.TransparentIndex, h.Pixel(2, 2));
    }

    [AvaloniaFact]
    public void ShapesAndOtherBrushes_DrawWithoutErrors()
    {
        using var h = new Harness(24, 24);
        foreach (var k in new[] { ToolKind.Line, ToolKind.Ellipse, ToolKind.Spray, ToolKind.Dither, ToolKind.Shading, ToolKind.LightenDarken, ToolKind.Lasso, ToolKind.EllipseSelect, ToolKind.MagicWand })
        {
            h.Window.Tools.Current = k;
            h.Drag((2, 2), (10, 6), (14, 14));
        }
        h.Window.Tools.Current = ToolKind.Ellipse;
        h.Window.Tools.SetFilled(true);
        h.Window.Canvas.CancelGesture();
        h.Key(Key.D, RawInputModifiers.Control);
        h.Drag((16, 16), (22, 22));
        Assert.Equal(Black, h.Pixel(19, 19));
        Assert.NotNull(h.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void TrimmedCel_GrowsToCanvasWhenDrawn()
    {
        using var h = new Harness(10, 10);
        var cel = new Cel(new Image(2, 2), 4, 4);
        h.Doc.Cels[(h.Session.Layer, 0)] = cel;
        h.Drag((0, 0));
        var now = h.Doc.Cels[(h.Session.Layer, 0)];
        Assert.Equal((0, 0, 10, 10), (now.X, now.Y, now.Image.Width, now.Image.Height));
        Assert.Equal(Black, h.Pixel(0, 0));
        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal((4, 4, 2), (now.X, now.Y, now.Image.Width));
    }
}
