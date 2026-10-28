using Avalonia;
using Avalonia.Media;

namespace EasyPixel;

/// <summary>The theme's palette (Theme/*.axaml) for code that draws or builds controls.</summary>
internal static class Res
{
    public static IBrush Brush(string key) =>
        Application.Current != null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var v) && v is IBrush b
            ? b
            : throw new KeyNotFoundException("No theme resource '" + key + "'.");

    public static Color Color(string key) => Brush(key) is ISolidColorBrush s ? s.Color : Colors.Transparent;
}
