using Avalonia.Interactivity;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel;

// Select menu: all, none, invert, and every pixel of the primary colour on the current layer.
public partial class MainWindow
{
    private void Invert_Click(object? sender, RoutedEventArgs e) => InvertSelection();
    private void SelectColor_Click(object? sender, RoutedEventArgs e) => SelectByColor(Tools.Primary);

    internal void InvertSelection()
    {
        Session.DropFloating();
        Session.Sel = Session.Sel.Invert();
    }

    /// <summary>
    /// Selects every pixel of the current layer that has <paramref name="color"/> (within the magic wand's tolerance),
    /// wherever it is: the magic wand without "contiguous", started from a colour instead of a pixel.
    /// </summary>
    internal void SelectByColor(Rgba color)
    {
        Session.DropFloating();
        var doc = Session.Doc;
        var sel = Selection.None(doc.Width, doc.Height);
        if (doc.Cels.TryGetValue((Session.Layer, Session.Frame), out var cel) && PixelValueOf(doc, color) is { } value)
            sel = SelectionTools.FromImageSpace(Selectors.ByValue(cel.Image, value, Tools.Tolerance), cel.X, cel.Y, doc.Width, doc.Height);
        Session.Sel = sel;
        ShowStatus(sel.IsEmpty ? L.T("No pixel has that colour on this layer") : L.T("Selected every pixel of the primary colour"));
    }

    /// <summary>The value an image stores for <paramref name="c"/> (an existing palette index in indexed mode), or null.</summary>
    private static uint? PixelValueOf(Sprite doc, Rgba c)
    {
        if (doc.Mode == ColorMode.Rgba) return c.Packed;
        if (c.A == 0 && doc.Palette.TransparentIndex >= 0) return (uint)doc.Palette.TransparentIndex;
        int i = doc.Palette.IndexOf(c);
        return i >= 0 ? (uint)i : null;
    }
}
