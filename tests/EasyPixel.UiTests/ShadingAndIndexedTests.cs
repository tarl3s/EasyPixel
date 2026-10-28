using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Tools;
using Image = EasyPixel.Core.Model.Image;

namespace EasyPixel.UiTests;

public class ShadingAndIndexedTests
{
    private static readonly (int, int)[] BackAndForth = { (2, 5), (12, 5), (2, 5), (12, 5), (3, 6), (11, 4) };

    [AvaloniaFact]
    public void Shading_Brush3_BackAndForth_MovesExactlyOneStep()
    {
        using var h = new Harness(16, 16);
        var ramp = h.Window.Tools.DefaultRamp.Colors;
        var cel = h.Session.CelForDrawing();
        Array.Fill(cel.Image.Pixels, ramp[3].Packed);
        h.Window.Canvas.RefreshAll();
        h.Window.Tools.Current = ToolKind.Shading;
        h.Window.Tools.SetBrush(3, BrushShape.Square);

        h.Drag(BackAndForth);
        Assert.Equal(ramp[4].Packed, h.Pixel(7, 5));
        Assert.Equal(ramp[4].Packed, h.Pixel(7, 4));
        Assert.Equal(ramp[3].Packed, h.Pixel(7, 9)); // outside the brush

        h.Drag(MouseButton.Right, RawInputModifiers.None, BackAndForth); // a new stroke darkens once
        Assert.Equal(ramp[3].Packed, h.Pixel(7, 5));
    }

    [AvaloniaFact]
    public void LightenDarken_Brush3_BackAndForth_ChangesOnce()
    {
        using var h = new Harness(16, 16);
        var start = new Rgba(100, 60, 40, 255).Packed;
        Array.Fill(h.Session.CelForDrawing().Image.Pixels, start);
        h.Window.Tools.Current = ToolKind.LightenDarken;
        h.Window.Tools.SetBrush(3, BrushShape.Square);
        h.Drag(BackAndForth);

        var once = new Image(1, 1, start);
        Shading.LightenDarken(new PixelSink(once), 0, 0, new Brush(), h.Window.Tools.LightenAmount);
        Assert.NotEqual(start, once.Pixels[0]);
        Assert.Equal(once.Pixels[0], h.Pixel(7, 5));
        Assert.Equal(once.Pixels[0], h.Pixel(5, 6));
    }

    [AvaloniaFact]
    public void Indexed_Shading_LeavesTransparentAlone()
    {
        using var h = new Harness(16, 16, ColorMode.Indexed);
        var pal = h.Doc.Palette; // index 0 = transparent
        pal.Colors.Add(new Rgba(40, 40, 40, 255));
        pal.Colors.Add(new Rgba(120, 120, 120, 255));
        var cel = h.Session.CelForDrawing();
        cel.Image.Set(7, 5, 2);
        cel.Image.Set(8, 5, 1);
        h.Window.Tools.Current = ToolKind.Shading;
        h.Window.Tools.SetBrush(3, BrushShape.Square);

        h.Drag(MouseButton.Right, RawInputModifiers.None, BackAndForth); // darken
        Assert.Equal(1u, h.Pixel(7, 5));
        Assert.Equal(1u, h.Pixel(8, 5)); // the darkest colour doesn't turn into the transparent entry
        Assert.Equal(0u, h.Pixel(5, 5));

        h.Drag(BackAndForth); // lighten: transparent pixels stay transparent
        Assert.Equal(0u, h.Pixel(5, 5));
        Assert.Equal(2u, h.Pixel(7, 5));
    }

    /// <summary>An indexed sprite whose transparent entry is 3 (index 0 is a real colour).</summary>
    private static Harness IndexedWithTransparent3()
    {
        var h = new Harness(10, 10);
        var s = new Sprite(10, 10, ColorMode.Indexed);
        s.Palette.Colors.Clear();
        for (int i = 0; i < 5; i++) s.Palette.Colors.Add(i == 3 ? Rgba.Transparent : new Rgba((byte)(i * 50), 10, 10, 255));
        s.Palette.TransparentIndex = 3;
        var cel = new Cel(new Image(10, 10, 3));
        s.Cels[(s.Layers[0], 0)] = cel;
        cel.Image.Set(2, 2, 0);
        cel.Image.Set(3, 2, 1);
        h.Window.SetSession(new EditorSession(s));
        Harness.Pump();
        h.Window.Canvas.FitToView();
        return h;
    }

    [AvaloniaFact]
    public void Indexed_TransparentIndex3_MoveKeepsColour0AndHoles()
    {
        using var h = IndexedWithTransparent3();
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((1, 1), (3, 3));
        h.Window.Tools.Current = ToolKind.Move;
        h.Drag((2, 2), (2, 4));
        Assert.Equal(3u, h.Pixel(2, 2)); // the lifted spot is transparent, not colour 0
        Assert.NotNull(h.Window.CaptureRenderedFrame()); // floating picture with a non-zero transparent value
        h.Key(Key.Enter);

        Assert.Equal(0u, h.Pixel(2, 4)); // colour 0 moved like any colour
        Assert.Equal(1u, h.Pixel(3, 4));
        Assert.Equal(3u, h.Pixel(2, 2));
        Assert.Equal(3u, h.Pixel(1, 3)); // a hole in the floating pixels didn't overwrite anything
        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Equal(0u, h.Pixel(2, 2));
        Assert.Equal(3u, h.Pixel(2, 4));
    }

    [AvaloniaFact]
    public async Task Indexed_TransparentIndex3_PasteSkipsTransparent_EscRestoresSelection()
    {
        using var h = IndexedWithTransparent3();
        h.Session.CelForDrawing().Image.Set(6, 7, 4);
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((1, 1), (3, 3));
        await h.Window.CopyAsync();

        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Drag((5, 5), (7, 7));
        var before = h.Session.Sel;
        await h.Window.PasteAsync(); // at the selection: the copied (1,1)-(3,3) lands on (5,5)-(7,7)
        h.Key(Key.Escape);
        Assert.Null(h.Session.Float);
        Assert.Same(before, h.Session.Sel);
        Assert.Equal(4u, h.Pixel(6, 7));

        await h.Window.PasteAsync();
        h.Key(Key.Enter);
        Assert.Equal(0u, h.Pixel(6, 6)); // copied colour 0
        Assert.Equal(1u, h.Pixel(7, 6));
        Assert.Equal(4u, h.Pixel(6, 7)); // transparent pasted pixels left it alone
        Assert.Equal(3u, h.Pixel(5, 5));
    }

    [AvaloniaFact]
    public async Task Cut_WithNothingSelected_DoesNothing()
    {
        using var h = new Harness(8, 8);
        h.Drag((1, 1));
        var clip = MainWindow.InternalClipboard;
        await h.Window.CutAsync();
        Assert.Same(clip, MainWindow.InternalClipboard);
        Assert.Equal(new Rgba(0, 0, 0, 255).Packed, h.Pixel(1, 1));
    }

    [AvaloniaFact]
    public void KeyDuringStroke_EndsTheStrokeFirst()
    {
        using var h = new Harness(10, 10);
        h.Window.MouseDown(h.At(1, 1), MouseButton.Left, RawInputModifiers.None);
        h.Window.MouseMove(h.At(4, 1), RawInputModifiers.None);
        h.Key(Key.Z, RawInputModifiers.Control); // ends the stroke (one undo step), then undoes it
        h.Window.MouseUp(h.At(4, 1), MouseButton.Left, RawInputModifiers.None);
        Harness.Pump();
        Assert.Equal(0u, h.Pixel(2, 1));
        Assert.True(h.Session.History.CanRedo);
    }
}
