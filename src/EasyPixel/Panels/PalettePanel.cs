using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Formats;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;

namespace EasyPixel.Panels;

/// <summary>
/// The sprite's palette as a grid of swatches: left click picks the primary colour, right click the secondary.
/// Entries can be added (the primary colour), removed and moved; palettes load from and save to .gpl/.hex/.pal files.
/// Every change is one undo step; in an indexed sprite removing or moving an entry renumbers the pixels so the picture
/// keeps its colours.
/// </summary>
public sealed class PalettePanel : DockPanel
{
    /// <summary>The palette new RGBA sprites get: DawnBringer's 16 colours.</summary>
    public static readonly Rgba[] DefaultColors = new uint[]
    {
        0x140c1c, 0x442434, 0x30346d, 0x4e4a4e, 0x854c30, 0x346524, 0xd04648, 0x757161,
        0x597dce, 0xd27d2c, 0x8595a1, 0x6daa2c, 0xd2aa99, 0x6dc2ca, 0xdad45e, 0xdeeed6,
    }.Select(v => new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255)).ToArray();

    private readonly WrapPanel _grid = new() { Orientation = Orientation.Horizontal };
    private readonly List<Border> _swatches = new();
    private MainWindow? _w;
    private int _selected = -1;

    public PalettePanel()
    {
        Margin = new Thickness(8, 0, 8, 8);
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 4, 0, 0) };
        bar.Children.Add(new TextBlock { Text = L.T("Palette"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        bar.Children.Add(Small("＋", L.T("Add the primary colour"), () => AddPrimary()));
        bar.Children.Add(Small("－", L.T("Remove the selected colour"), () => RemoveSelected()));
        bar.Children.Add(Small("◀", L.T("Move the selected colour left"), () => MoveSelected(-1)));
        bar.Children.Add(Small("▶", L.T("Move the selected colour right"), () => MoveSelected(+1)));
        var more = Small("⋯", L.T("Palette files"), () => { });
        more.ContextMenu = new ContextMenu
        {
            ItemsSource = new[]
            {
                Item(L.T("Load palette…"), () => _ = LoadAsync()),
                Item(L.T("Save palette…"), () => _ = SaveAsync()),
            },
        };
        more.Click += (_, _) => more.ContextMenu.Open(more);
        bar.Children.Add(more);
        SetDock(bar, Dock.Top);
        Children.Add(bar);
        Children.Add(new ScrollViewer { Content = _grid, Margin = new Thickness(0, 6, 0, 0), HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
    }

    /// <summary>Index of the selected entry, or -1.</summary>
    public int Selected => _selected;

    private Sprite Doc => _w!.Session.Doc;

    public void Attach(MainWindow w)
    {
        _w = w;
        Rebuild();
    }

    internal static Button Small(string glyph, string tip, Action click)
    {
        var b = new Button { Content = glyph, Padding = new Thickness(6, 1), MinWidth = 26, Focusable = false, HorizontalContentAlignment = HorizontalAlignment.Center };
        ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        return b;
    }

    internal static MenuItem Item(string header, Action click, bool enabled = true)
    {
        var m = new MenuItem { Header = header, IsEnabled = enabled };
        m.Click += (_, _) => click();
        return m;
    }

    /// <summary>Redraws the swatches from the document's palette.</summary>
    public void Rebuild()
    {
        if (_w == null) return;
        var pal = Doc.Palette;
        if (_selected >= pal.Colors.Count) _selected = pal.Colors.Count - 1;
        while (_swatches.Count < pal.Colors.Count)
        {
            int index = _swatches.Count;
            var b = new Border { Width = 16, Height = 16, Margin = new Thickness(1), BorderThickness = new Thickness(1) };
            b.PointerPressed += (_, e) => Pressed(index, e);
            _swatches.Add(b);
        }
        _grid.Children.Clear();
        for (int i = 0; i < pal.Colors.Count; i++)
        {
            var b = _swatches[i];
            var c = pal.Colors[i];
            bool transparent = i == pal.TransparentIndex || c.A == 0;
            b.Background = transparent ? Res.Brush("Bg0") : new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            b.Child = transparent ? new TextBlock { Text = "×", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } : null;
            b.BorderBrush = i == _selected ? Res.Brush("Accent") : Res.Brush("Border");
            b.BorderThickness = new Thickness(i == _selected ? 2 : 1);
            ToolTip.SetTip(b, L.F("Colour {0}: {1}", i, MainWindow.Hex(c)));
            _grid.Children.Add(b);
        }
    }

    private void Pressed(int index, PointerPressedEventArgs e)
    {
        if (_w == null || index >= Doc.Palette.Colors.Count) return;
        var c = Doc.Palette.Colors[index];
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) _w.Tools.SetSecondary(c);
        else _w.Tools.SetPrimary(c);
        Select(index);
        e.Handled = true;
    }

    internal void Select(int index)
    {
        _selected = index;
        Rebuild();
    }

    // ---- changes (each one undo step) ----

    internal void AddPrimary()
    {
        if (_w == null) return;
        if (Doc.Palette.Colors.Count >= Palette.MaxColors) { _w.ShowStatus(L.T("The palette is full (256 colours)")); return; }
        var c = _w.Tools.Primary;
        _w.Session.Apply(PaletteEdit.Perform(Doc, L.N("Add colour"), () => Doc.Palette.Add(c)));
        Select(Doc.Palette.Colors.Count - 1);
    }

    internal void RemoveSelected()
    {
        if (_w == null) return;
        if (_selected < 0) { _w.ShowStatus(L.T("Pick a colour in the palette first")); return; }
        int i = _selected;
        if (Doc.Mode == ColorMode.Indexed)
        {
            if (Doc.Palette.Colors.Count == 1) { _w.ShowStatus(L.T("The last colour can't be removed")); return; }
            _w.Session.Apply(SpriteStateEdit.Perform(Doc, L.N("Remove colour"), () => Doc.RemovePaletteEntry(i)));
        }
        else _w.Session.Apply(PaletteEdit.Perform(Doc, L.N("Remove colour"), () => Doc.Palette.Colors.RemoveAt(i)));
        Select(Math.Min(i, Doc.Palette.Colors.Count - 1));
    }

    internal void MoveSelected(int delta)
    {
        if (_w == null) return;
        if (_selected < 0) { _w.ShowStatus(L.T("Pick a colour in the palette first")); return; }
        int from = _selected, to = from + delta;
        if (to < 0 || to >= Doc.Palette.Colors.Count) return;
        _w.Session.Apply(Doc.Mode == ColorMode.Indexed
            ? SpriteStateEdit.Perform(Doc, L.N("Move colour"), () => Doc.MovePaletteEntry(from, to))
            : PaletteEdit.Perform(Doc, L.N("Move colour"), () => Doc.MovePaletteEntry(from, to)));
        Select(to);
    }

    private static IReadOnlyList<FileType> Types => new[]
    {
        new FileType(L.T("GIMP palette"), new[] { ".gpl" }), new FileType(L.T("Hex list"), new[] { ".hex" }),
        new FileType(L.T("JASC palette"), new[] { ".pal" }),
    };

    /// <summary>Replaces the palette with the colours of a file. Indexed pixels keep their numbers (as in Aseprite).</summary>
    internal async Task LoadAsync(string? path = null)
    {
        if (_w == null) return;
        path ??= await _w.Dialogs.PickOpenFile(Types);
        if (path == null) return;
        List<Rgba> colors;
        try { colors = PaletteFormats.Read(path); }
        catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
        {
            await _w.Dialogs.Tell(L.F("{0} could not be opened:\n{1}", Path.GetFileName(path), ex.Message));
            return;
        }
        var doc = Doc;
        _w.Session.Apply(PaletteEdit.Perform(doc, L.N("Load palette"), () =>
        {
            int t = doc.Palette.TransparentIndex;
            doc.Palette.Colors.Clear();
            doc.Palette.Colors.AddRange(colors);
            doc.Palette.TransparentIndex = t < colors.Count ? t : -1;
        }));
        _selected = -1;
        Rebuild();
        _w.ShowStatus(L.F("Loaded {0} colours", colors.Count));
    }

    internal async Task SaveAsync(string? path = null)
    {
        if (_w == null) return;
        path ??= await _w.Dialogs.PickSaveFile(L.T("Save palette"), "palette.gpl", Types);
        if (path == null) return;
        var ms = new MemoryStream();
        try
        {
            PaletteFormats.Write(ms, Path.GetExtension(path), Doc.Palette.Colors, Path.GetFileNameWithoutExtension(path));
            MainWindow.WriteAtomically(path, ms);
        }
        catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
        {
            await _w.Dialogs.Tell(L.F("The file could not be written:\n{0}", ex.Message));
            return;
        }
        _w.ShowStatus(L.F("Saved {0}", Path.GetFileName(path)));
    }
}
