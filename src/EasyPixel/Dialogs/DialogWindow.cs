using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace EasyPixel.Dialogs;

/// <summary>A small modal window: content on top, a row of buttons below; <see cref="Result"/> is the index of the button pressed (-1 = closed).</summary>
public class DialogWindow : Window
{
    private readonly StackPanel _body = new() { Spacing = 10 };
    private readonly StackPanel _buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };

    public DialogWindow(string title)
    {
        Title = title;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new StackPanel { Margin = new Thickness(20), Spacing = 18, MinWidth = 320 };
        root.Children.Add(_body);
        root.Children.Add(_buttons);
        Content = root;
    }

    protected override Type StyleKeyOverride => typeof(Window);

    public int Result { get; private set; } = -1;

    protected void AddText(string text) =>
        _body.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 440 });

    protected void AddRow(string label, Control field)
    {
        var row = new DockPanel();
        var l = new TextBlock { Text = label, Width = 120, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(l, Dock.Left);
        row.Children.Add(l);
        row.Children.Add(field);
        _body.Children.Add(row);
    }

    protected void AddControl(Control c) => _body.Children.Add(c);

    /// <summary>Adds a button; <paramref name="isDefault"/> answers Enter, <paramref name="isCancel"/> answers Escape.</summary>
    public Button AddButton(string text, bool isDefault = false, bool isCancel = false)
    {
        int index = _buttons.Children.Count;
        var b = new Button { Content = text, MinWidth = 90, IsDefault = isDefault, IsCancel = isCancel, HorizontalContentAlignment = HorizontalAlignment.Center };
        b.Click += (_, _) =>
        {
            if (!Validate(index)) return;
            Result = index;
            Close();
        };
        _buttons.Children.Add(b);
        return b;
    }

    /// <summary>Lets a dialog refuse a button (e.g. invalid input); true by default.</summary>
    protected virtual bool Validate(int button) => true;

    /// <summary>A plain message with the given buttons.</summary>
    public static DialogWindow Message(string title, string message, params string[] buttons)
    {
        var w = new DialogWindow(title);
        w.AddText(message);
        for (int i = 0; i < buttons.Length; i++) w.AddButton(buttons[i], isDefault: i == 0, isCancel: i == buttons.Length - 1);
        return w;
    }
}
