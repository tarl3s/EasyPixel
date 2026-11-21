using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;
using EasyPixel.Tools;
using PixelRect = EasyPixel.Core.Model.PixelRect;

namespace EasyPixel.UiTests;

public class FlipButtonsTests
{
    private static readonly uint Red = new Rgba(255, 0, 0, 255).Packed;

    private static Button Find(Harness h, string startsWith) =>
        h.Window.OptionsBar.Children.OfType<Button>().First(b => (b.Content as string)?.StartsWith(startsWith) == true);

    [AvaloniaFact]
    public void SelectionTools_ShowFlipAndTurnButtons_ThatChangeOnlyTheSelection()
    {
        using var h = new Harness(16, 16);
        var img = h.Doc.GetOrCreateCel(h.Session.Layer, 0).Image;
        img.Set(2, 2, Red);   // inside the selection (2..5, 2..3)
        img.Set(10, 10, Red); // outside it
        h.Session.InvalidateAll();
        h.Window.Tools.Current = ToolKind.RectSelect;
        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(2, 2, 4, 2));
        Harness.Pump();

        Find(h, "⇆").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        h.Key(Key.Enter);
        Assert.Equal(Red, h.Pixel(5, 2));   // mirrored inside the box
        Assert.Equal(0u, h.Pixel(2, 2));
        Assert.Equal(Red, h.Pixel(10, 10)); // the rest of the layer untouched

        h.Session.Sel = Selectors.Rect(16, 16, new PixelRect(2, 2, 4, 2));
        Find(h, "⇅").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        h.Key(Key.Enter);
        Assert.Equal(Red, h.Pixel(5, 3));
        Assert.Equal(Red, h.Pixel(10, 10));
    }

    [AvaloniaFact]
    public void PaintTools_DontShowThem()
    {
        using var h = new Harness();
        h.Window.Tools.Current = ToolKind.Pencil;
        Harness.Pump();
        Assert.DoesNotContain(h.Window.OptionsBar.Children.OfType<Button>(), b => (b.Content as string)?.StartsWith("⇆") == true);
    }
}
