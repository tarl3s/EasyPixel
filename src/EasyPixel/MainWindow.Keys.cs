using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyPixel.Tools;

namespace EasyPixel;

// Keyboard: the File/Edit/View shortcuts, Space to pan, arrows to nudge, Enter to drop, Escape to cancel.
// (Tool letters and the rest of Aseprite's shortcut set come with the panels.)
public partial class MainWindow
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        e.Handled = HandleKey(e.Key, e.KeyModifiers, e.Source is TextBox);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.Space) Canvas.SpaceHeld = false;
    }

    /// <summary>Runs the shortcut for a key; false when the key means nothing here.</summary>
    internal bool HandleKey(Key key, KeyModifiers mods, bool inTextBox = false)
    {
        bool ctrl = mods.HasFlag(KeyModifiers.Control), shift = mods.HasFlag(KeyModifiers.Shift);
        if (ctrl)
        {
            switch (key)
            {
                case Key.Z when shift: Redo(); return true;
                case Key.Z: Undo(); return true;
                case Key.Y: Redo(); return true;
                case Key.N: _ = NewAsync(); return true;
                case Key.O: _ = OpenAsync(); return true;
                case Key.S when shift: _ = SaveAsAsync(); return true;
                case Key.S: _ = SaveAsync(); return true;
                case Key.E: _ = ExportAsync(); return true;
                case Key.D0 or Key.NumPad0: Canvas.FitToView(); return true;
                case Key.OemPlus or Key.Add: Canvas.ZoomStep(1); return true;
                case Key.OemMinus or Key.Subtract: Canvas.ZoomStep(-1); return true;
            }
            if (inTextBox) return false; // the text box's own cut/copy/paste/select all
            switch (key)
            {
                case Key.X: _ = CutAsync(); return true;
                case Key.C: _ = CopyAsync(); return true;
                case Key.V: _ = PasteAsync(); return true;
                case Key.A: SelectAll(); return true;
                case Key.D: Deselect(); return true;
            }
            return false;
        }
        if (inTextBox) return false;
        int step = shift ? 10 : 1;
        switch (key)
        {
            case Key.Space: Canvas.SpaceHeld = true; return true;
            case Key.Delete or Key.Back: DeleteSelection(); return true;
            case Key.Enter: Session.DropFloating(); return true;
            case Key.Escape:
                if (!Canvas.CancelGesture()) Session.CancelFloating();
                return true;
            case Key.Left or Key.Right or Key.Up or Key.Down when Tools.Current == ToolKind.Move || Session.Float != null:
                if (Canvas.Context is { } ctx)
                    MoveTool.Nudge(ctx, key == Key.Left ? -step : key == Key.Right ? step : 0, key == Key.Up ? -step : key == Key.Down ? step : 0);
                UpdateTitle();
                return true;
        }
        return false;
    }

    // ---- View menu ----

    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => Canvas.ZoomStep(1);
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => Canvas.ZoomStep(-1);
    private void Fit_Click(object? sender, RoutedEventArgs e) => Canvas.FitToView();

    private void Grid_Click(object? sender, RoutedEventArgs e)
    {
        Canvas.ShowGrid = GridItem.IsChecked;
        App.Settings.ShowGrid = Canvas.ShowGrid;
        App.Settings.Save();
    }

    private void Onion_Click(object? sender, RoutedEventArgs e) => Canvas.ShowOnion = OnionItem.IsChecked;
}
