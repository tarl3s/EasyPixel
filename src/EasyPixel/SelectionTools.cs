using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel;

/// <summary>Selection helpers the editor needs on top of Core's <see cref="Selection"/>.</summary>
internal static class SelectionTools
{
    /// <summary>Combines a new shape with the current selection: Shift adds, Alt subtracts, otherwise it replaces.</summary>
    public static Selection Combine(Selection current, Selection shape, SelectMode mode) => mode switch
    {
        SelectMode.Add => current.Union(shape),
        SelectMode.Subtract => current.Subtract(shape),
        _ => shape,
    };
}

public enum SelectMode { Replace, Add, Subtract }
