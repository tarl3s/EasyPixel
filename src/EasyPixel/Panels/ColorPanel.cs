using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using EasyPixel.Tools;

namespace EasyPixel.Panels;

/// <summary>The right-hand colour panel: the picker for the primary colour and its hex code.</summary>
public sealed class ColorPanel : StackPanel
{
    private ToolController? _tools;

    public ColorPanel()
    {
        Spacing = 6;
        Margin = new Thickness(8);
        Picker = new ColorPicker();
        Hex = new TextBox { FontSize = 12, Width = 96, HorizontalAlignment = HorizontalAlignment.Left };
        ToolTip.SetTip(Hex, L.T("Colour code: #RRGGBB or #RRGGBBAA"));
        var row = new DockPanel();
        var label = new TextBlock { Text = L.T("Colour"), Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        DockPanel.SetDock(label, Dock.Left);
        row.Children.Add(label);
        row.Children.Add(Hex);
        Children.Add(Picker);
        Children.Add(row);
        Picker.ColorPicked += c => _tools?.SetPrimary(c);
        Hex.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            ApplyHex();
            e.Handled = true;
        };
        Hex.LostFocus += (_, _) => ApplyHex();
    }

    public ColorPicker Picker { get; }
    public TextBox Hex { get; }

    public void Attach(ToolController tools)
    {
        _tools = tools;
        tools.Changed += Sync;
        Sync();
    }

    private void Sync()
    {
        if (_tools == null) return;
        Picker.Color = _tools.Primary;
        if (!Hex.IsFocused) Hex.Text = MainWindow.Hex(_tools.Primary);
    }

    private void ApplyHex()
    {
        if (_tools == null) return;
        if (MainWindow.TryParseHex(Hex.Text, out var c)) _tools.SetPrimary(c);
        else Hex.Text = MainWindow.Hex(_tools.Primary);
    }
}
