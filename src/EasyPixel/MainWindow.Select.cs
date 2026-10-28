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
    /// wherever it is: the magic wand without "contiguous".
    /// </summary>
    internal void SelectByColor(Rgba color)
    {
        Session.DropFloating();
        var doc = Session.Doc;
        var sel = Selection.None(doc.Width, doc.Height);
        if (doc.Cels.TryGetValue((Session.Layer, Session.Frame), out var cel))
        {
            int tol = Tools.Tolerance;
            var img = cel.Image;
            for (int y = 0; y < img.Height; y++)
            {
                int cy = y + cel.Y;
                if ((uint)cy >= (uint)doc.Height) continue;
                for (int x = 0; x < img.Width; x++)
                {
                    int cx = x + cel.X;
                    if ((uint)cx >= (uint)doc.Width) continue;
                    var c = Rgba.FromPacked(ToRgba(doc, img.Pixels[y * img.Width + x]));
                    if (Near(c, color, tol)) sel.Set(cx, cy, true);
                }
            }
        }
        Session.Sel = sel;
        ShowStatus(sel.IsEmpty ? L.T("No pixel has that colour on this layer") : L.T("Selected every pixel of the primary colour"));
    }

    private static bool Near(Rgba a, Rgba b, int tol) =>
        (a.A == 0 && b.A == 0)
        || (Math.Abs(a.R - b.R) <= tol && Math.Abs(a.G - b.G) <= tol && Math.Abs(a.B - b.B) <= tol && Math.Abs(a.A - b.A) <= tol);
}
