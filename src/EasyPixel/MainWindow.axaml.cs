using Avalonia.Controls;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Tools;

namespace EasyPixel;

/// <summary>
/// The editor window: menu, tool options, tool strip, canvas and status bar. Split by area: File (new/open/save/export,
/// asking to save), Edit (undo, clipboard, selection), Tools (strip, options, colours), Keys, View.
/// </summary>
public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Dialogs = new AppDialogs(this);
        Tools.Changed += OnToolsChanged;
        Canvas.ViewChanged += UpdateStatusBar;
        Canvas.ShowGrid = App.Settings.ShowGrid;
        GridItem.IsChecked = App.Settings.ShowGrid;
        BuildToolStrip();
        HexBox.KeyDown += HexBox_KeyDown;
        HexBox.LostFocus += (_, _) => ApplyHex();
        BuildRecentMenu();
        SetSession(new EditorSession(new Sprite(32, 32)));
        OnToolsChanged();
    }

    /// <summary>The open document.</summary>
    public EditorSession Session { get; private set; } = null!;

    public ToolController Tools { get; } = new();

    /// <summary>Where questions go; the UI tests swap in scripted answers.</summary>
    internal IDialogs Dialogs { get; set; }

    internal void SetSession(EditorSession s)
    {
        if (Session != null) Session.HistoryChanged -= UpdateTitle;
        Session = s;
        s.HistoryChanged += UpdateTitle;
        Canvas.Attach(s, Tools, ShowStatus);
        UpdateTitle();
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
