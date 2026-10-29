using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Panels;
using EasyPixel.Tools;

namespace EasyPixel;

/// <summary>
/// The editor window: menu, tool options, tool strip, canvas, colour/palette/layers panels, timeline and status bar.
/// One partial per menu or area: File, Edit, Sprite, Layer, Frame, Select, View (also Settings and Help), Tools (strip,
/// options, colours), Keys (shortcuts). The panels themselves live in Panels/.
/// </summary>
public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Dialogs = new AppDialogs(this);
        CrashGuard.Window = this;
        Tools.Changed += OnToolsChanged;
        Canvas.ViewChanged += UpdateStatusBar;
        BuildToolStrip();
        ToolTip.SetTip(SwapButton, L.T("Swap primary and secondary colours") + " (X)");
        BuildRecentMenu();
        ApplyViewSettings();
        UpdateThemeMenu(App.Settings.Theme);
        Colors.Attach(Tools);
        SetSession(new EditorSession(NewDocument(32, 32, ColorMode.Rgba)));
        PaletteView.Attach(this);
        Layers.Attach(this);
        Timeline.Attach(this);
        AttachLessonStrip();
        OnToolsChanged();
        // Tab hides the panels (before focus navigation takes it), unless someone is typing.
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Tab || e.Source is TextBox || e.KeyModifiers != KeyModifiers.None) return;
            TogglePanels();
            e.Handled = true;
        }, RoutingStrategies.Tunnel);
    }

    /// <summary>The open document.</summary>
    public EditorSession Session { get; private set; } = null!;

    public ToolController Tools { get; } = new();

    /// <summary>Where questions go; the UI tests swap in scripted answers.</summary>
    internal IDialogs Dialogs { get; set; }

    internal void SetSession(EditorSession s)
    {
        if (CurrentPractice != null && s != CurrentPractice.Session) EndPractice(); // another document: the lesson bar goes
        StopPlaying();
        if (Session != null)
        {
            Session.HistoryChanged -= OnHistoryChanged;
            Session.CurrentChanged -= OnCurrentChanged;
        }
        Session = s;
        _rangeAnchor = s.Frame;
        s.HistoryChanged += OnHistoryChanged;
        s.CurrentChanged += OnCurrentChanged;
        Canvas.Attach(s, Tools, ShowStatus);
        UpdateTitle();
        UpdateStatusBar();
        RefreshPanels();
    }

    /// <summary>A new sprite as File → New makes it: RGBA ones get a starting palette to pick colours from.</summary>
    internal static Sprite NewDocument(int width, int height, ColorMode mode)
    {
        var s = new Sprite(width, height, mode);
        if (mode == ColorMode.Rgba) s.Palette.Colors.AddRange(PalettePanel.DefaultColors);
        else s.Palette.Colors.AddRange(PalettePanel.DefaultColors.Take(Palette.MaxColors - 1));
        return s;
    }

    /// <summary>Another frame or layer became current: only what shows the current one is updated.</summary>
    private void OnCurrentChanged()
    {
        if (Session.Layer != _layerShown) { _layerShown = Session.Layer; Layers.Rebuild(); }
        Timeline.Highlight();
        UpdateStatusBar();
    }
    private Layer? _layerShown;

    private void OnHistoryChanged()
    {
        if (IsPlaying && Session.Doc.Frames.Count != _playFrames) StopPlaying();
        UpdateTitle();
        RefreshPanels();
    }

    /// <summary>Brings the panels, timeline and menus up to date with the document (after any change).</summary>
    internal void RefreshPanels()
    {
        if (Session == null) return;
        if (_rangeAnchor >= Session.Doc.Frames.Count) _rangeAnchor = Session.Frame;
        _layerShown = Session.Layer;
        Layers?.Rebuild();
        Timeline?.Rebuild();
        PaletteView?.Rebuild();
        UpdateModeMenu();
        UpdateStatusBar();
    }

    private void UpdateTitle()
    {
        Title = $"{Session.DisplayName}{(Session.History.IsDirty ? " *" : "")} - EasyPixel";
        UndoItem.Header = Session.History.UndoName is { } u ? L.F("Undo {0}", L.T(u)) : L.T("Undo");
        RedoItem.Header = Session.History.RedoName is { } r ? L.F("Redo {0}", L.T(r)) : L.T("Redo");
        UndoItem.IsEnabled = Session.History.CanUndo || Session.Float != null;
        RedoItem.IsEnabled = Session.History.CanRedo;
    }

    internal void ShowStatus(string message) => StatusText.Text = message;

    private void UpdateStatusBar()
    {
        PosText.Text = Canvas.Hover is { } h ? $"{h.X}, {h.Y}" : "";
        ZoomText.Text = L.F("{0}%   {1} x {2}", Math.Round(Canvas.ZoomPercent, 1), Session.Doc.Width, Session.Doc.Height);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || e.Cancel) return;
        if (!Session.History.IsDirty && Session.Float == null)
        {
            _closeConfirmed = true;
            return;
        }
        e.Cancel = true; // ask first; closes again once the answer allows it
        _ = CloseAfterAskingAsync();
    }

    private async Task CloseAfterAskingAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        _closeConfirmed = true;
        Close();
    }
}
