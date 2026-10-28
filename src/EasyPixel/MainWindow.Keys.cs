using Avalonia.Controls;
using Avalonia.Input;
using EasyPixel.Tools;

namespace EasyPixel;

// Keyboard: Aseprite's default shortcuts. The list shown in Help → Keyboard shortcuts is Shortcuts below.
public partial class MainWindow
{
    /// <summary>Every shortcut and what it does (English; translated when shown).</summary>
    internal static readonly (string Keys, string Action)[] Shortcuts =
    {
        ("B", "Pencil"), ("E", "Eraser"), ("G", "Bucket"), ("L", "Line"), ("U", "Rectangle / ellipse (press again to switch)"),
        ("M", "Rectangle / ellipse selection (press again to switch)"), ("Q", "Lasso"), ("W", "Magic wand"), ("V", "Move"),
        ("I", "Eyedropper"), ("Alt", "Eyedropper while held (with a drawing tool)"), ("X", "Swap primary and secondary colours"),
        ("Ctrl+Z", "Undo"), ("Ctrl+Y / Ctrl+Shift+Z", "Redo"), ("Ctrl+N", "New…"), ("Ctrl+O", "Open…"), ("Ctrl+S", "Save"),
        ("Ctrl+Shift+S", "Save as…"), ("Ctrl+E", "Export…"), ("Ctrl+X / Ctrl+C / Ctrl+V", "Cut / copy / paste"),
        ("Delete", "Delete"), ("Ctrl+A", "Select all"), ("Ctrl+D", "Deselect"), ("Ctrl+Shift+I", "Invert selection"),
        ("Alt+N", "New frame"), ("Shift+N", "New layer"), ("← / →", "Previous / next frame"), ("↑ / ↓", "Layer above / below"),
        ("Enter", "Play / stop (drops moved pixels first)"), ("Esc", "Cancel"), ("Tab", "Hide / show panels"), ("F", "Full preview"),
        ("Space + drag", "Scroll the canvas"), ("Ctrl++ / Ctrl+-", "Zoom in / out"), ("Ctrl+0", "Fit on screen"),
        ("Shift + arrows", "Move pixels 10 at a time (Move tool)"),
    };

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
        bool ctrl = mods.HasFlag(KeyModifiers.Control), shift = mods.HasFlag(KeyModifiers.Shift), alt = mods.HasFlag(KeyModifiers.Alt);
        // A key during a drag: Escape abandons it, anything but a modifier or Space ends it first, so no command runs
        // in the middle of a stroke (strokes share one backup buffer and keep their own undo step).
        bool modifierOnly = key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.Space;
        if (!modifierOnly && !inTextBox)
        {
            if (key == Key.Escape) { if (Canvas.CancelGesture()) return true; }
            else Canvas.FinishGesture();
        }
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
                case Key.I when shift: InvertSelection(); return true;
                case Key.X: _ = CutAsync(); return true;
                case Key.C: _ = CopyAsync(); return true;
                case Key.V: _ = PasteAsync(); return true;
                case Key.A: SelectAll(); return true;
                case Key.D: Deselect(); return true;
            }
            return false;
        }
        if (inTextBox) return false;
        if (alt)
        {
            if (key == Key.N) { NewFrame(); return true; }
            return false;
        }
        int step = shift ? 10 : 1;
        bool moving = Tools.Current == ToolKind.Move || Session.Float != null;
        switch (key)
        {
            case Key.Space: Canvas.SpaceHeld = true; return true;
            case Key.Delete or Key.Back: DeleteSelection(); return true;
            case Key.Enter:
                if (Session.Float != null) Session.DropFloating();
                else TogglePlay();
                return true;
            case Key.Escape:
                if (IsPlaying) StopPlaying();
                else if (!Canvas.CancelGesture()) Session.CancelFloating();
                return true;
            case Key.Left or Key.Right or Key.Up or Key.Down when moving:
                if (Canvas.Context is { } ctx)
                    MoveTool.Nudge(ctx, key == Key.Left ? -step : key == Key.Right ? step : 0, key == Key.Up ? -step : key == Key.Down ? step : 0);
                UpdateTitle();
                return true;
            case Key.Left: GoToFrame(Session.Frame - 1); return true;
            case Key.Right: GoToFrame(Session.Frame + 1); return true;
            case Key.Up: StepLayer(+1); return true;
            case Key.Down: StepLayer(-1); return true;
            case Key.Tab: TogglePanels(); return true;
            case Key.N when shift: NewLayer(); return true;
        }
        if (shift) return false;
        switch (key)
        {
            case Key.B: Tools.Current = ToolKind.Pencil; return true;
            case Key.E: Tools.Current = ToolKind.Eraser; return true;
            case Key.G: Tools.Current = ToolKind.Bucket; return true;
            case Key.L: Tools.Current = ToolKind.Line; return true;
            case Key.U: Tools.Current = Tools.Current == ToolKind.Rectangle ? ToolKind.Ellipse : ToolKind.Rectangle; return true;
            case Key.M: Tools.Current = Tools.Current == ToolKind.RectSelect ? ToolKind.EllipseSelect : ToolKind.RectSelect; return true;
            case Key.Q: Tools.Current = ToolKind.Lasso; return true;
            case Key.W: Tools.Current = ToolKind.MagicWand; return true;
            case Key.V: Tools.Current = ToolKind.Move; return true;
            case Key.I: Tools.Current = ToolKind.Eyedropper; return true;
            case Key.X: Tools.SwapColors(); return true;
            case Key.F: ShowPreview(full: true); return true;
        }
        return false;
    }

    /// <summary>Up/Down: the layer above or below the current one, as listed in the layers panel.</summary>
    private void StepLayer(int direction)
    {
        var shown = Layers.Shown;
        int i = shown.ToList().IndexOf(Session.Layer);
        int j = i - direction; // the panel lists the top layer first
        if (i < 0 || j < 0 || j >= shown.Count) return;
        Session.Layer = shown[j];
    }

    /// <summary>The letter shown after a tool's name in its tooltip, or null.</summary>
    internal static string? ToolKey(ToolKind k) => k switch
    {
        ToolKind.Pencil => "B", ToolKind.Eraser => "E", ToolKind.Bucket => "G", ToolKind.Line => "L",
        ToolKind.Rectangle or ToolKind.Ellipse => "U", ToolKind.RectSelect or ToolKind.EllipseSelect => "M", ToolKind.Lasso => "Q",
        ToolKind.MagicWand => "W", ToolKind.Move => "V", ToolKind.Eyedropper => "I", _ => null,
    };
}
