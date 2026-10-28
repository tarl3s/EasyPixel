using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Tools;

namespace EasyPixel;

// The tool strip, the current tool's options bar and the colour swatches.
public partial class MainWindow
{
    /// <summary>Strip order and symbols (symbols are pictures, not words; the tooltip carries the name).</summary>
    private static readonly (ToolKind Kind, string Glyph)[] Strip =
    {
        (ToolKind.Pencil, "✎"), (ToolKind.Eraser, "⌫"), (ToolKind.Bucket, "◪"), (ToolKind.Eyedropper, "⊙"),
        (ToolKind.Line, "╱"), (ToolKind.Rectangle, "▭"), (ToolKind.Ellipse, "◯"), (ToolKind.Spray, "⁘"),
        (ToolKind.Shading, "◐"), (ToolKind.LightenDarken, "☼"), (ToolKind.Dither, "▦"), (ToolKind.Move, "✥"),
        (ToolKind.RectSelect, "⬚"), (ToolKind.EllipseSelect, "◌"), (ToolKind.Lasso, "➰"), (ToolKind.MagicWand, "✧"),
    };

    private readonly Dictionary<ToolKind, ToggleButton> _toolButtons = new();
    private ToolKind? _optionsFor;

    private void BuildToolStrip()
    {
        foreach (var (kind, glyph) in Strip)
        {
            var b = new ToggleButton { Content = glyph, Classes = { "tool" }, Focusable = false }; // Space and arrows stay with the canvas
            ToolTip.SetTip(b, ToolKey(kind) is { } key ? L.T(Tools.Get(kind).Name) + " (" + key + ")" : L.T(Tools.Get(kind).Name));
            b.Click += (_, _) => Tools.Current = kind;
            _toolButtons[kind] = b;
            ToolStrip.Children.Add(b);
        }
    }

    private void OnToolsChanged()
    {
        foreach (var (kind, b) in _toolButtons) b.IsChecked = kind == Tools.Current;
        if (_optionsFor != Tools.Current)
        {
            if (Tools.Current != ToolKind.Move) Session?.DropFloating();
            _optionsFor = Tools.Current;
            BuildOptions();
        }
        PrimarySwatch.Background = new SolidColorBrush(ToColor(Tools.Primary));
        SecondarySwatch.Background = new SolidColorBrush(ToColor(Tools.Secondary));
        Canvas.InvalidateVisual(); // mirror axes
    }

    private void BuildOptions()
    {
        var bar = OptionsBar.Children;
        bar.Clear();
        var k = Tools.Current;
        bar.Add(new TextBlock { Text = L.T(Tools.Get(k).Name), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });

        bool brush = k is ToolKind.Pencil or ToolKind.Eraser or ToolKind.Line or ToolKind.Rectangle or ToolKind.Ellipse
            or ToolKind.Shading or ToolKind.LightenDarken or ToolKind.Dither;
        if (brush)
        {
            var size = new NumericUpDown { Value = Tools.Brush.Size, Minimum = 1, Maximum = 64, Increment = 1, FormatString = "0", Width = 110 };
            size.ValueChanged += (_, e) => Tools.SetBrush((int)(e.NewValue ?? 1), Tools.Brush.Shape);
            bar.Add(Labeled(L.T("Size"), size));
            var shape = new ComboBox { ItemsSource = new[] { L.T("Square"), L.T("Circle") }, SelectedIndex = (int)Tools.Brush.Shape };
            shape.SelectionChanged += (_, _) => Tools.SetBrush(Tools.Brush.Size, (BrushShape)Math.Max(0, shape.SelectedIndex));
            bar.Add(shape);
        }
        if (k is ToolKind.Pencil or ToolKind.Eraser)
            bar.Add(Check(L.T("Pixel perfect"), Tools.PixelPerfect, v => Tools.SetPixelPerfect(v)));
        if (k is ToolKind.Rectangle or ToolKind.Ellipse)
            bar.Add(Check(L.T("Filled"), Tools.Filled, v => Tools.SetFilled(v)));
        if (k is ToolKind.Bucket or ToolKind.MagicWand)
        {
            bar.Add(Check(L.T("Contiguous"), Tools.Contiguous, v => Tools.SetFillOptions(v, Tools.Tolerance)));
            var tol = new NumericUpDown { Value = Tools.Tolerance, Minimum = 0, Maximum = 255, Increment = 1, FormatString = "0", Width = 110 };
            tol.ValueChanged += (_, e) => Tools.SetFillOptions(Tools.Contiguous, (int)(e.NewValue ?? 0));
            bar.Add(Labeled(L.T("Tolerance"), tol));
        }
        if (k == ToolKind.Dither)
        {
            var pattern = new ComboBox { ItemsSource = new[] { L.T("Checker"), L.T("Bayer 2x2"), L.T("Bayer 4x4"), L.T("Bayer 8x8") }, SelectedIndex = (int)Tools.Dither };
            pattern.SelectionChanged += (_, _) => Tools.SetDither((DitherKind)Math.Max(0, pattern.SelectedIndex));
            bar.Add(Labeled(L.T("Pattern"), pattern));
        }
        if (k == ToolKind.Spray)
        {
            var radius = new NumericUpDown { Value = Tools.SprayRadius, Minimum = 1, Maximum = 64, Increment = 1, FormatString = "0", Width = 110 };
            radius.ValueChanged += (_, e) => Tools.SprayRadius = (int)(e.NewValue ?? 6);
            bar.Add(Labeled(L.T("Radius"), radius));
        }
        if (k is ToolKind.Shading or ToolKind.LightenDarken)
            bar.Add(Hint(L.T("Left click lightens, right click darkens")));
        if (k is ToolKind.RectSelect or ToolKind.EllipseSelect or ToolKind.Lasso or ToolKind.MagicWand)
            bar.Add(Hint(L.T("Shift adds to the selection, Alt subtracts")));
        if (k == ToolKind.Move)
            bar.Add(Hint(L.T("Drag the selection; arrow keys nudge; Enter drops")));
        if (ToolController.IsPaintTool(k))
        {
            bar.Add(Check(L.T("Mirror horizontally"), Tools.MirrorH, v => Tools.SetMirror(v, Tools.MirrorV)));
            bar.Add(Check(L.T("Mirror vertically"), Tools.MirrorV, v => Tools.SetMirror(Tools.MirrorH, v)));
        }
    }

    private static Control Labeled(string label, Control field)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        p.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        p.Children.Add(field);
        return p;
    }

    private static CheckBox Check(string label, bool value, Action<bool> set)
    {
        var c = new CheckBox { Content = label, IsChecked = value, VerticalAlignment = VerticalAlignment.Center };
        c.IsCheckedChanged += (_, _) => set(c.IsChecked == true);
        return c;
    }

    private static TextBlock Hint(string text) =>
        new() { Text = text, Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center };

    private void Swap_Click(object? sender, RoutedEventArgs e) => Tools.SwapColors();

    /// <summary>Reads "#RRGGBB" or "#RRGGBBAA" (the # is optional).</summary>
    internal static bool TryParseHex(string? text, out Rgba c)
    {
        c = default;
        var s = (text ?? "").Trim().TrimStart('#');
        if ((s.Length != 6 && s.Length != 8) || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return false;
        if (s.Length == 6) v = v << 8 | 0xFF;
        c = new Rgba((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        return true;
    }

    internal static string Hex(Rgba c) => c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    private static Color ToColor(Rgba c) => Color.FromArgb(c.A, c.R, c.G, c.B);
}
