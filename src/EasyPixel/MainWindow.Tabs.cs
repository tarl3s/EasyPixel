using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace EasyPixel;

// Tabs: several sprites open at once, one tab each, above the canvas. New, Open, Recent, Import and "Try it" open a
// tab (the untouched starting sprite is replaced instead of piling up); each tab keeps its own undo history, file and
// selection. Closing a tab with changes asks to save it; closing the window asks for every tab with changes.
public partial class MainWindow
{
    private readonly List<EditorSession> _tabs = new();

    /// <summary>The open sprites, in tab order.</summary>
    internal IReadOnlyList<EditorSession> Tabs => _tabs;

    /// <summary>
    /// Shows <paramref name="s"/> in a new tab (or its own tab, when it's already open). The starting sprite, while
    /// nobody has drawn on it or saved it, is replaced instead of keeping an empty tab around.
    /// </summary>
    internal void OpenTab(EditorSession s)
    {
        if (!_tabs.Contains(s))
        {
            int at = _tabs.Count;
            if (Session != null && IsUntouched(Session) && _tabs.IndexOf(Session) is var i && i >= 0)
            {
                _tabs.RemoveAt(i);
                at = i;
            }
            _tabs.Insert(at, s);
        }
        SetSession(s);
        BuildTabs();
    }

    /// <summary>
    /// A picture from a format with no palette (PNG, GIF, Piskel, an engine sprite) gets the starting palette, so the
    /// palette panel has colours to pick from instead of going empty. Only colours to pick from: the picture doesn't
    /// change and it isn't an unsaved change.
    /// </summary>
    internal static void GivePaletteIfNone(Core.Model.Sprite s)
    {
        if (s.Mode == Core.Model.ColorMode.Rgba && s.Palette.Colors.Count == 0) s.Palette.Colors.AddRange(Panels.PalettePanel.StartColors);
    }

    private static bool IsUntouched(EditorSession s) =>
        s.Path == null && !s.History.IsDirty && !s.History.CanUndo && s.Float == null;

    /// <summary>The tab already showing <paramref name="path"/>, if any.</summary>
    private EditorSession? TabFor(string path)
    {
        var full = Path.GetFullPath(path);
        return _tabs.FirstOrDefault(t => t.Path != null && string.Equals(t.Path, full, StringComparison.OrdinalIgnoreCase));
    }

    internal void SwitchTab(EditorSession s)
    {
        if (s == Session || !_tabs.Contains(s)) return;
        Session.DropFloating(); // floating pixels belong to the tab they were lifted in
        SetSession(s);
        BuildTabs();
    }

    /// <summary>Ctrl+Tab / Ctrl+Shift+Tab: the next or previous tab, round.</summary>
    internal void StepTab(int by)
    {
        if (_tabs.Count < 2) return;
        int i = (_tabs.IndexOf(Session) + by + _tabs.Count) % _tabs.Count;
        SwitchTab(_tabs[i]);
    }

    /// <summary>Closes a tab, asking to save it first when it has changes. The last tab closed leaves a fresh sprite. True when it closed.</summary>
    internal async Task<bool> CloseTabAsync(EditorSession s)
    {
        if (!_tabs.Contains(s)) return false;
        if (s != Session) SwitchTab(s); // the question names it, and the person sees what they're closing
        if (!await ConfirmDiscardAsync()) return false;
        int i = _tabs.IndexOf(s);
        _tabs.RemoveAt(i);
        if (_tabs.Count == 0) _tabs.Add(new EditorSession(NewDocument(32, 32, Core.Model.ColorMode.Rgba)));
        SetSession(_tabs[Math.Min(i, _tabs.Count - 1)]);
        BuildTabs();
        return true;
    }

    /// <summary>Before the window closes: asks about every tab with changes, one by one. False = the person cancelled.</summary>
    private async Task<bool> ConfirmDiscardAllAsync()
    {
        foreach (var t in _tabs.ToList())
        {
            if (!t.History.IsDirty && t.Float == null) continue;
            SwitchTab(t);
            if (!await ConfirmDiscardAsync()) return false;
        }
        return true;
    }

    private bool AnyTabChanged => _tabs.Any(t => t.History.IsDirty || t.Float != null);

    /// <summary>Redraws the tab bar: one button per sprite (name, * when changed, ✕ to close), the current one highlighted.</summary>
    internal void BuildTabs()
    {
        if (TabBar == null) return;
        TabBar.Children.Clear();
        foreach (var t in _tabs)
        {
            var tab = t;
            bool current = tab == Session;
            var name = new TextBlock { Text = tab.DisplayName + (tab.History.IsDirty ? " *" : ""), VerticalAlignment = VerticalAlignment.Center, MaxWidth = 220, TextTrimming = TextTrimming.CharacterEllipsis };
            L.Keep(name); // a file name, never translated
            var fore = current ? Res.Brush("AccentText") : Res.Brush("Text");
            var close = new Button { Content = "✕", Padding = new Thickness(5, 0), Margin = new Thickness(6, 0, 0, 0), FontSize = 11, Background = current ? Res.Brush("Accent") : Res.Brush("Bg2"), Foreground = fore, Focusable = false, VerticalAlignment = VerticalAlignment.Center };
            ToolTip.SetTip(close, L.T("Close this sprite") + " (Ctrl+W)");
            close.Click += (_, e) => { e.Handled = true; _ = CloseTabAsync(tab); };
            var body = new StackPanel { Orientation = Orientation.Horizontal };
            body.Children.Add(name);
            body.Children.Add(close);
            var button = new Button
            {
                Content = body, Padding = new Thickness(10, 3, 4, 3), Margin = new Thickness(0, 0, 2, 0), Focusable = false,
                Background = current ? Res.Brush("Accent") : Res.Brush("Bg2"),
                Foreground = fore,
            };
            if (tab.Path != null) ToolTip.SetTip(button, tab.Path);
            button.Click += (_, _) => SwitchTab(tab);
            // the middle button closes a tab, as in browsers
            button.PointerReleased += (_, e) => { if (e.InitialPressMouseButton == MouseButton.Middle) _ = CloseTabAsync(tab); };
            TabBar.Children.Add(button);
        }
    }
}
