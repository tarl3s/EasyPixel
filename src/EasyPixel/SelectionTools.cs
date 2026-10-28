using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel;

/// <summary>Selection helpers the editor needs on top of Core's <see cref="Selection"/>.</summary>
internal static class SelectionTools
{
    /// <summary>The same pixels moved by (dx, dy); pixels pushed off the canvas are dropped.</summary>
    public static Selection Translate(Selection s, int dx, int dy)
    {
        if (dx == 0 && dy == 0) return s;
        var r = new Selection(s.Width, s.Height);
        var b = s.Bounds;
        for (int y = b.Y; y < b.Bottom; y++)
            for (int x = b.X; x < b.Right; x++)
                if (s.Contains(x, y)) r.Set(x + dx, y + dy, true);
        return r;
    }

    /// <summary>A selection of <paramref name="w"/> x <paramref name="h"/> from one built over an image placed at (offsetX, offsetY).</summary>
    public static Selection FromImageSpace(Selection imageSel, int offsetX, int offsetY, int w, int h)
    {
        var r = new Selection(w, h);
        var b = imageSel.Bounds;
        for (int y = b.Y; y < b.Bottom; y++)
            for (int x = b.X; x < b.Right; x++)
                if (imageSel.Contains(x, y)) r.Set(x + offsetX, y + offsetY, true);
        return r;
    }

    /// <summary>Combines a new shape with the current selection: Shift adds, Alt subtracts, otherwise it replaces.</summary>
    public static Selection Combine(Selection current, Selection shape, SelectMode mode) => mode switch
    {
        SelectMode.Add => current.Union(shape),
        SelectMode.Subtract => current.Subtract(shape),
        _ => shape,
    };
}

public enum SelectMode { Replace, Add, Subtract }
