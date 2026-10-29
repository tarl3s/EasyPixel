using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using EasyPixel.Core.Model;

namespace EasyPixel.Dialogs;

/// <summary>One whole number (pixelate cell size, colours to keep, frame duration…).</summary>
internal sealed class NumberWindow : DialogWindow
{
    private readonly NumericUpDown _value;

    public NumberWindow(string title, string label, int value, int min, int max) : base(title)
    {
        _value = NewSpriteWindow.Number(value, min, max);
        AddRow(label, _value);
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    public int Value => (int)(_value.Value ?? 0);
}

/// <summary>A name.</summary>
internal sealed class TextWindow : DialogWindow
{
    private readonly TextBox _text;

    public TextWindow(string title, string label, string value) : base(title)
    {
        _text = L.Keep(new TextBox { Text = value, MinWidth = 220 });
        AddRow(label, _text);
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
        Opened += (_, _) => { _text.Focus(); _text.SelectAll(); };
    }

    public string Value => _text.Text ?? "";

    protected override bool Validate(int button) => button != 0 || !string.IsNullOrWhiteSpace(_text.Text);
}

/// <summary>A width and a height, optionally kept in proportion.</summary>
internal class SizeWindow : DialogWindow
{
    protected readonly NumericUpDown W, H;
    private readonly CheckBox _ratio = new() { Content = L.T("Keep proportions"), IsChecked = true };
    private readonly double _aspect;
    private bool _syncing;

    public SizeWindow(string title, int width, int height, bool offerRatio = true) : base(title)
    {
        W = NewSpriteWindow.Number(width);
        H = NewSpriteWindow.Number(height);
        _aspect = (double)width / height;
        AddRow(L.T("Width"), W);
        AddRow(L.T("Height"), H);
        if (offerRatio)
        {
            AddControl(_ratio);
            W.ValueChanged += (_, _) => Sync(fromWidth: true);
            H.ValueChanged += (_, _) => Sync(fromWidth: false);
        }
        else _ratio.IsChecked = false;
    }

    protected void AddOkCancel()
    {
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    private void Sync(bool fromWidth)
    {
        if (_syncing || _ratio.IsChecked != true) return;
        _syncing = true;
        if (fromWidth) H.Value = (decimal)Math.Max(1, Math.Round((double)(W.Value ?? 1) / _aspect));
        else W.Value = (decimal)Math.Max(1, Math.Round((double)(H.Value ?? 1) * _aspect));
        _syncing = false;
    }

    public (int Width, int Height) Size => ((int)(W.Value ?? 1), (int)(H.Value ?? 1));

    public static SizeWindow Simple(string title, int width, int height, bool offerRatio)
    {
        var w = new SizeWindow(title, width, height, offerRatio);
        w.AddOkCancel();
        return w;
    }
}

/// <summary>File → Import sprite sheet: cell width and height, and the gap between cells.</summary>
internal sealed class SliceWindow : SizeWindow
{
    private readonly NumericUpDown _padding = NewSpriteWindow.Number(0, 0, 1024);

    public SliceWindow(int width, int height) : base(L.T("Import sprite sheet"), width, height, offerRatio: false)
    {
        AddRow(L.T("Gap between cells"), _padding);
        AddOkCancel();
    }

    public SliceOptions Options => new(Size.Width, Size.Height, (int)(_padding.Value ?? 0));
}

/// <summary>Sprite → Canvas size: width, height and a 3×3 anchor (where the old picture stays).</summary>
internal sealed class CanvasSizeWindow : SizeWindow
{
    private Anchor _anchor = Anchor.Center;

    public CanvasSizeWindow(int width, int height) : base(L.T("Canvas size"), width, height, offerRatio: false)
    {
        var grid = new UniformGrid { Rows = 3, Columns = 3, Width = 102, HorizontalAlignment = HorizontalAlignment.Left };
        var buttons = new List<ToggleButton>();
        foreach (Anchor a in Enum.GetValues<Anchor>())
        {
            var b = new ToggleButton { Content = Arrow(a), Width = 32, Height = 32, Margin = new Avalonia.Thickness(1), IsChecked = a == _anchor, HorizontalContentAlignment = HorizontalAlignment.Center };
            ToolTip.SetTip(b, AnchorName(a));
            var anchor = a;
            b.Click += (_, _) =>
            {
                _anchor = anchor;
                foreach (var o in buttons) o.IsChecked = o == b;
            };
            buttons.Add(b);
            grid.Children.Add(b);
        }
        AddRow(L.T("Anchor"), grid);
        AddOkCancel();
    }

    private static string AnchorName(Anchor a) => a switch
    {
        Anchor.TopLeft => L.T("Top left"), Anchor.Top => L.T("Top"), Anchor.TopRight => L.T("Top right"), Anchor.Left => L.T("Left"),
        Anchor.Center => L.T("Centre"), Anchor.Right => L.T("Right"), Anchor.BottomLeft => L.T("Bottom left"), Anchor.Bottom => L.T("Bottom"),
        _ => L.T("Bottom right"),
    };

    private static string Arrow(Anchor a) => a switch
    {
        Anchor.TopLeft => "↖", Anchor.Top => "↑", Anchor.TopRight => "↗", Anchor.Left => "←", Anchor.Center => "•",
        Anchor.Right => "→", Anchor.BottomLeft => "↙", Anchor.Bottom => "↓", _ => "↘",
    };

    public CanvasSizeOptions Options => new(Size.Width, Size.Height, _anchor);
}

/// <summary>Frame → Onion skin settings.</summary>
internal sealed class OnionWindow : DialogWindow
{
    private readonly NumericUpDown _before, _after, _opacity;
    private readonly CheckBox _tint;

    public OnionWindow(OnionOptions o) : base(L.T("Onion skin"))
    {
        _before = NewSpriteWindow.Number(o.Before, 0, 16);
        _after = NewSpriteWindow.Number(o.After, 0, 16);
        _opacity = NewSpriteWindow.Number(o.Opacity, 1, 255);
        _tint = new CheckBox { Content = L.T("Tint: red before, blue after"), IsChecked = o.Tint };
        AddRow(L.T("Frames before"), _before);
        AddRow(L.T("Frames after"), _after);
        AddRow(L.T("Opacity"), _opacity);
        AddControl(_tint);
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    public OnionOptions Options => new((int)(_before.Value ?? 1), (int)(_after.Value ?? 1), (int)(_opacity.Value ?? 68), _tint.IsChecked == true);
}

/// <summary>Edit → Settings: language, theme (both apply after a restart) and update checks.</summary>
internal sealed class SettingsWindow : DialogWindow
{
    private readonly ComboBox _language, _theme;
    private readonly CheckBox _updates;
    private readonly List<string> _codes = new() { "auto" };

    public SettingsWindow(SettingsChoice current) : base(L.T("Settings"))
    {
        var items = new List<object> { new ComboBoxItem { Content = L.T("Automatic (system language)") } };
        foreach (var (code, name) in L.Languages)
        {
            _codes.Add(code);
            items.Add(L.Keep(new ComboBoxItem { Content = name }));
        }
        _language = new ComboBox { ItemsSource = items, SelectedIndex = Math.Max(0, _codes.IndexOf(current.Language)), MinWidth = 220 };
        _theme = new ComboBox { ItemsSource = new[] { L.T("Dark"), L.T("Light") }, SelectedIndex = current.Theme == "light" ? 1 : 0, MinWidth = 220 };
        AddRow(L.T("Language"), _language);
        AddRow(L.T("Theme"), _theme);
        _updates = new CheckBox { Content = L.T("Check for updates when EasyPixel starts"), IsChecked = current.CheckForUpdates };
        AddControl(_updates);
        AddText(L.T("Changes apply the next time EasyPixel starts."));
        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    public SettingsChoice Choice => new(_codes[Math.Max(0, _language.SelectedIndex)], _theme.SelectedIndex == 1 ? "light" : "dark", _updates.IsChecked == true);
}

/// <summary>Help → Keyboard shortcuts.</summary>
internal sealed class ShortcutsWindow : DialogWindow
{
    public ShortcutsWindow(IReadOnlyList<(string Keys, string Action)> shortcuts) : base(L.T("Keyboard shortcuts"))
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Avalonia.Thickness(0, 0, 0, 4) };
        for (int i = 0; i < shortcuts.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var k = new TextBlock { Text = shortcuts[i].Keys, FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 2, 18, 2) };
            var a = new TextBlock { Text = shortcuts[i].Action, Margin = new Avalonia.Thickness(0, 2) };
            Grid.SetRow(k, i);
            Grid.SetRow(a, i);
            Grid.SetColumn(a, 1);
            grid.Children.Add(k);
            grid.Children.Add(a);
        }
        AddControl(new ScrollViewer { Content = grid, MaxHeight = 520 });
        AddButton(L.T("OK"), isDefault: true, isCancel: true);
    }
}
