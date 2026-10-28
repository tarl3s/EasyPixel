using Avalonia.Controls;
using Avalonia.Input;

namespace EasyPixel.Dialogs;

/// <summary>Pick one line from a list (a project, a sprite of a project). Double-click or Enter picks too.</summary>
internal sealed class ChooseWindow : DialogWindow
{
    private readonly ListBox _list;

    /// <param name="options">Already translated, or the person's own names: shown as they are.</param>
    public ChooseWindow(string title, string message, IReadOnlyList<string> options) : base(title)
    {
        AddText(message);
        _list = new ListBox { MinWidth = 380, MaxHeight = 320, SelectedIndex = options.Count > 0 ? 0 : -1 };
        foreach (var o in options) _list.Items.Add(L.Keep(new TextBlock { Text = o }));
        AddControl(_list);
        var ok = AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
        _list.DoubleTapped += (_, _) => { if (_list.SelectedIndex >= 0) ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); };
        Opened += (_, _) => _list.Focus();
    }

    public int Index => _list.SelectedIndex;

    protected override bool Validate(int button) => button != 0 || _list.SelectedIndex >= 0;
}
