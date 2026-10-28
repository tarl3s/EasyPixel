using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Model;

namespace EasyPixel.Panels;

/// <summary>
/// The layers, topmost first, with groups indented under their group (which folds open and shut). Each row has an
/// eye (show/hide) and a lock; a click makes the layer current, a double click renames it, the right button opens its
/// menu. Under the list: the current layer's opacity and blend mode, and buttons to add, copy, remove and move layers.
/// </summary>
public sealed class LayersPanel : DockPanel
{
    private readonly StackPanel _rows = new();
    private readonly NumericUpDown _opacity = new() { Minimum = 0, Maximum = 255, Increment = 1, FormatString = "0", Width = 110 };
    private readonly ComboBox _blend = new() { MinWidth = 120 };
    private readonly HashSet<Layer> _collapsed = new();
    private Sprite? _doc;
    private MainWindow? _w;
    private bool _syncing;

    public LayersPanel()
    {
        Margin = new Thickness(8, 0, 8, 8);
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 6, 0, 4) };
        head.Children.Add(new TextBlock { Text = L.T("Layers"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        head.Children.Add(PalettePanel.Small("＋", L.T("New layer (Shift+N)"), () => _w?.NewLayer()));
        head.Children.Add(PalettePanel.Small("▣", L.T("New group"), () => _w?.NewGroup()));
        head.Children.Add(PalettePanel.Small("⧉", L.T("Duplicate layer"), () => _w?.DuplicateLayer()));
        head.Children.Add(PalettePanel.Small("－", L.T("Remove layer"), () => _w?.RemoveLayer()));
        head.Children.Add(PalettePanel.Small("▲", L.T("Move layer up"), () => _w?.MoveLayer(+1)));
        head.Children.Add(PalettePanel.Small("▼", L.T("Move layer down"), () => _w?.MoveLayer(-1)));
        SetDock(head, Dock.Top);
        Children.Add(head);

        var props = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };
        props.Children.Add(Row(L.T("Opacity"), _opacity));
        props.Children.Add(Row(L.T("Blend"), _blend));
        SetDock(props, Dock.Bottom);
        Children.Add(props);
        Children.Add(new ScrollViewer { Content = _rows });

        _blend.ItemsSource = Enum.GetValues<BlendMode>().Select(BlendName).ToArray();
        _opacity.ValueChanged += (_, e) =>
        {
            if (!_syncing && _w != null) _w.SetLayerOpacity(_w.Session.Layer, (int)(e.NewValue ?? 255));
        };
        _blend.SelectionChanged += (_, _) =>
        {
            if (!_syncing && _w != null && _blend.SelectedIndex >= 0) _w.SetLayerBlend(_w.Session.Layer, (BlendMode)_blend.SelectedIndex);
        };
    }

    private static Control Row(string label, Control field)
    {
        var d = new DockPanel();
        var t = new TextBlock { Text = label, Width = 64, VerticalAlignment = VerticalAlignment.Center };
        SetDock(t, Dock.Left);
        d.Children.Add(t);
        d.Children.Add(field);
        return d;
    }

    public static string BlendName(BlendMode m) => m switch
    {
        BlendMode.Normal => L.T("Normal"), BlendMode.Multiply => L.T("Multiply"), BlendMode.Screen => L.T("Screen"),
        BlendMode.Overlay => L.T("Overlay"), BlendMode.Darken => L.T("Darken"), BlendMode.Lighten => L.T("Lighten"),
        BlendMode.ColorDodge => L.T("Colour dodge"), BlendMode.ColorBurn => L.T("Colour burn"), BlendMode.HardLight => L.T("Hard light"),
        BlendMode.SoftLight => L.T("Soft light"), BlendMode.Difference => L.T("Difference"), BlendMode.Exclusion => L.T("Exclusion"),
        BlendMode.Hue => L.T("Hue"), BlendMode.Saturation => L.T("Saturation"), BlendMode.Color => L.T("Colour"),
        BlendMode.Luminosity => L.T("Luminosity"), BlendMode.Addition => L.T("Addition"), BlendMode.Subtract => L.T("Subtract"),
        _ => L.T("Divide"),
    };

    public void Attach(MainWindow w)
    {
        _w = w;
        Rebuild();
    }

    /// <summary>Rows in the order shown (top first), for the tests.</summary>
    internal IReadOnlyList<Layer> Shown { get; private set; } = Array.Empty<Layer>();

    public void Rebuild()
    {
        if (_w == null) return;
        var s = _w.Session;
        if (s.Doc != _doc)
        {
            // Another document: groups start folded as the file had them (Aseprite's collapsed flag).
            _doc = s.Doc;
            _collapsed.Clear();
            foreach (var g in _doc.Layers.Where(Core.Formats.AsepriteFormat.IsCollapsed)) _collapsed.Add(g);
        }
        var shown = new List<Layer>();
        _rows.Children.Clear();
        AddRows(s.Doc, null, 0, shown);
        Shown = shown;
        _syncing = true;
        _opacity.Value = s.Layer.Opacity;
        _blend.SelectedIndex = (int)s.Layer.Blend;
        _syncing = false;
    }

    private void AddRows(Sprite doc, Layer? parent, int depth, List<Layer> shown)
    {
        for (int i = doc.Layers.Count - 1; i >= 0; i--)
        {
            var l = doc.Layers[i];
            if (l.Parent != parent) continue;
            shown.Add(l);
            _rows.Children.Add(RowFor(l, depth));
            if (l.IsGroup && !_collapsed.Contains(l)) AddRows(doc, l, depth + 1, shown);
        }
    }

    private Control RowFor(Layer l, int depth)
    {
        var w = _w!;
        bool current = l == w.Session.Layer;
        var row = new DockPanel { Margin = new Thickness(depth * 14, 0, 0, 0) };
        var eye = Toggle(l.Visible ? "◉" : "○", L.T("Show or hide"), () => w.ToggleVisible(l));
        var lockBtn = Toggle(l.Locked ? "🔒" : "·", L.T("Lock or unlock"), () => w.ToggleLock(l));
        SetDock(eye, Dock.Left);
        SetDock(lockBtn, Dock.Left);
        row.Children.Add(eye);
        row.Children.Add(lockBtn);
        if (l.IsGroup)
        {
            var fold = Toggle(_collapsed.Contains(l) ? "▸" : "▾", L.T("Open or close the group"), () =>
            {
                if (!_collapsed.Remove(l)) _collapsed.Add(l);
                Rebuild();
            });
            SetDock(fold, Dock.Left);
            row.Children.Add(fold);
        }
        var name = L.Keep(new TextBlock
        {
            Text = l.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0),
            TextTrimming = TextTrimming.CharacterEllipsis, FontStyle = l.IsGroup ? FontStyle.Italic : FontStyle.Normal,
        });
        row.Children.Add(name);
        var border = new Border
        {
            Child = row, Padding = new Thickness(2, 1), CornerRadius = new CornerRadius(3),
            Background = current ? Res.Brush("Bg3") : Brushes.Transparent,
            BorderBrush = current ? Res.Brush("Accent") : Brushes.Transparent, BorderThickness = new Thickness(1),
        };
        border.PointerPressed += (_, e) =>
        {
            w.Session.Layer = l;
            if (e.ClickCount == 2 && e.GetCurrentPoint(border).Properties.IsLeftButtonPressed) _ = w.RenameLayerAsync(l);
        };
        border.ContextMenu = MenuFor(l);
        return border;
    }

    private static Button Toggle(string glyph, string tip, Action click)
    {
        var b = new Button
        {
            Content = glyph, Width = 22, Height = 22, Padding = new Thickness(0), Focusable = false, Background = Brushes.Transparent,
            HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(b, tip);
        b.Click += (_, e) => { click(); e.Handled = true; };
        return b;
    }

    private ContextMenu MenuFor(Layer l)
    {
        var w = _w!;
        var doc = w.Session.Doc;
        var into = new MenuItem { Header = L.T("Put into group") };
        var groups = doc.Layers.Where(g => g.IsGroup && g != l && !Core.Model.LayerTree.IsInside(g, l) && g != l.Parent).ToList();
        into.ItemsSource = groups.Select(g => L.Keep(PalettePanel.Item(g.Name, () => w.PutIntoGroup(l, g)))).ToList();
        into.IsEnabled = groups.Count > 0;
        return new ContextMenu
        {
            ItemsSource = new object[]
            {
                PalettePanel.Item(L.T("Rename…"), () => _ = w.RenameLayerAsync(l)),
                PalettePanel.Item(L.T("Duplicate layer"), () => { w.Session.Layer = l; w.DuplicateLayer(); }),
                PalettePanel.Item(L.T("Remove layer"), () => { w.Session.Layer = l; w.RemoveLayer(); }),
                new Separator(),
                PalettePanel.Item(L.T("Put in a new group"), () => { w.Session.Layer = l; w.GroupLayer(); }),
                into,
                PalettePanel.Item(L.T("Move out of group"), () => w.MoveOutOfGroup(l), l.Parent != null),
            },
        };
    }
}
