using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Model;

namespace EasyPixel.Panels;

/// <summary>
/// The timeline under the canvas: playback buttons, then the tags (coloured bars over their frames) and the frames
/// (number and duration). Click a frame to go to it, Shift+click to pick a range; the right button opens the frame or
/// tag menu; a double click on a frame edits its duration.
/// </summary>
public sealed class TimelinePanel : DockPanel
{
    public const double CellWidth = 46;

    private readonly Avalonia.Controls.Canvas _tags = new() { Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly StackPanel _frames = new() { Orientation = Orientation.Horizontal };
    private readonly Button _play;
    private readonly ToggleButton _loop, _onion;
    private MainWindow? _w;

    public TimelinePanel()
    {
        Margin = new Thickness(6, 4);
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Top };
        controls.Children.Add(PalettePanel.Small("⏮", L.T("First frame"), () => _w?.GoToFrame(0)));
        controls.Children.Add(PalettePanel.Small("◀", L.T("Previous frame"), () => _w?.GoToFrame(_w.Session.Frame - 1)));
        _play = PalettePanel.Small("▶", L.T("Play / stop (Enter)"), () => _w?.TogglePlay());
        controls.Children.Add(_play);
        controls.Children.Add(PalettePanel.Small("▶|", L.T("Next frame"), () => _w?.GoToFrame(_w.Session.Frame + 1)));
        controls.Children.Add(PalettePanel.Small("⏭", L.T("Last frame"), () => _w?.GoToFrame(int.MaxValue)));
        _loop = ToggleSmall("⟳", L.T("Loop"), on => _w?.SetLoop(on));
        _onion = ToggleSmall("◎", L.T("Onion skin"), on => _w?.SetOnion(on));
        controls.Children.Add(_loop);
        controls.Children.Add(_onion);
        controls.Children.Add(PalettePanel.Small("⚙", L.T("Onion skin settings…"), () => _ = _w?.OnionSettingsAsync()));
        controls.Children.Add(PalettePanel.Small("▢", L.T("Preview window"), () => _w?.ShowPreview(full: false)));
        controls.Children.Add(PalettePanel.Small("＋", L.T("New frame (Alt+N)"), () => _w?.NewFrame()));
        SetDock(controls, Dock.Left);
        Children.Add(controls);

        var lanes = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Left };
        lanes.Children.Add(_tags);
        lanes.Children.Add(_frames);
        Children.Add(new ScrollViewer { Content = lanes, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
    }

    private static ToggleButton ToggleSmall(string glyph, string tip, Action<bool> set)
    {
        var b = new ToggleButton { Content = glyph, Padding = new Thickness(6, 1), MinWidth = 26, Focusable = false, HorizontalContentAlignment = HorizontalAlignment.Center };
        ToolTip.SetTip(b, tip);
        b.IsCheckedChanged += (_, _) => set(b.IsChecked == true);
        return b;
    }

    public void Attach(MainWindow w)
    {
        _w = w;
        Rebuild();
    }

    /// <summary>Only what changes when another frame is current or playback starts/stops: the cell colours and the buttons.</summary>
    public void Highlight()
    {
        if (_w == null) return;
        var s = _w.Session;
        _play.Content = _w.IsPlaying ? "■" : "▶";
        _loop.IsChecked = _w.Playback.Loop;
        _onion.IsChecked = _w.Canvas.ShowOnion;
        var (from, to) = _w.FrameRange;
        int n = Math.Min(_frames.Children.Count, s.Doc.Frames.Count);
        for (int i = 0; i < n; i++)
        {
            var cell = (Border)_frames.Children[i];
            bool current = i == s.Frame, picked = i >= from && i <= to;
            cell.Background = current ? Res.Brush("Bg3") : picked ? Res.Brush("Hover") : Res.Brush("Bg2");
            cell.BorderBrush = current ? Res.Brush("Accent") : Res.Brush("Border");
        }
    }

    /// <summary>Everything: cells, durations, tags (after a change to the document).</summary>
    public void Rebuild()
    {
        if (_w == null) return;
        var doc = _w.Session.Doc;
        while (_frames.Children.Count > doc.Frames.Count) _frames.Children.RemoveAt(_frames.Children.Count - 1);
        while (_frames.Children.Count < doc.Frames.Count) _frames.Children.Add(NewCell(_frames.Children.Count));
        Highlight();
        for (int i = 0; i < doc.Frames.Count; i++)
        {
            var texts = (StackPanel)((Border)_frames.Children[i]).Child!;
            ((TextBlock)texts.Children[0]).Text = (i + 1).ToString();
            ((TextBlock)texts.Children[1]).Text = L.F("{0} ms", doc.Frames[i].DurationMs);
        }

        _tags.Children.Clear();
        _tags.Width = doc.Frames.Count * CellWidth;
        foreach (var tag in doc.Tags)
        {
            int a = Math.Clamp(tag.From, 0, doc.Frames.Count - 1), b = Math.Clamp(tag.To, a, doc.Frames.Count - 1);
            var bg = tag.Color.A == 0 ? Res.Color("Bg3") : Color.FromRgb(tag.Color.R, tag.Color.G, tag.Color.B);
            var bar = new Border
            {
                Width = (b - a + 1) * CellWidth - 2, Height = 18, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(bg),
                Child = L.Keep(new TextBlock
                {
                    Text = tag.Name + DirMark(tag.Dir), FontSize = 11, Margin = new Thickness(4, 0), VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis, Foreground = ReadableOn(bg),
                }),
            };
            ToolTip.SetTip(bar, L.F("Tag {0}: frames {1} to {2}", tag.Name, a + 1, b + 1));
            Avalonia.Controls.Canvas.SetLeft(bar, a * CellWidth + 1);
            var t = tag;
            bar.ContextMenu = TagMenu(t);
            bar.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(bar).Properties.IsLeftButtonPressed) return;
                _w.GoToFrame(t.From);
                _w.GoToFrame(t.To, extend: true);
            };
            _tags.Children.Add(bar);
        }
    }

    private static string DirMark(LoopDir d) => d switch { LoopDir.Reverse => " ◂", LoopDir.PingPong => " ◂▸", _ => "" };

    /// <summary>Black or white, whichever reads better on <paramref name="bg"/>.</summary>
    internal static IBrush ReadableOn(Color bg) =>
        Contrast.Ratio(bg, Colors.Black) >= Contrast.Ratio(bg, Colors.White) ? Brushes.Black : Brushes.White;

    private Border NewCell(int index)
    {
        var texts = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        texts.Children.Add(new TextBlock { FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        texts.Children.Add(new TextBlock { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center });
        var cell = new Border
        {
            Width = CellWidth - 2, Margin = new Thickness(1, 0), Padding = new Thickness(0, 3), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3), Child = texts, Cursor = new Cursor(StandardCursorType.Hand),
        };
        cell.PointerPressed += (_, e) =>
        {
            if (_w == null) return;
            var props = e.GetCurrentPoint(cell).Properties;
            if (props.IsLeftButtonPressed)
            {
                _w.GoToFrame(index, extend: e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                if (e.ClickCount == 2) _ = _w.FrameDurationAsync();
            }
            else if (props.IsRightButtonPressed)
            {
                var (from, to) = _w.FrameRange;
                if (index < from || index > to) _w.GoToFrame(index);
            }
        };
        cell.ContextMenu = FrameMenu();
        return cell;
    }

    private ContextMenu FrameMenu() => new()
    {
        ItemsSource = new object[]
        {
            PalettePanel.Item(L.T("New frame"), () => _w?.NewFrame()),
            PalettePanel.Item(L.T("New empty frame"), () => _w?.NewEmptyFrame()),
            PalettePanel.Item(L.T("Remove frame"), () => _w?.RemoveFrame()),
            new Separator(),
            PalettePanel.Item(L.T("Move frame left"), () => _w?.MoveFrame(-1)),
            PalettePanel.Item(L.T("Move frame right"), () => _w?.MoveFrame(+1)),
            PalettePanel.Item(L.T("Frame duration…"), () => _ = _w?.FrameDurationAsync()),
            new Separator(),
            PalettePanel.Item(L.T("New tag from the picked frames…"), () => _ = _w?.NewTagAsync()),
        },
    };

    private ContextMenu TagMenu(Tag t)
    {
        var w = _w!;
        var dir = new MenuItem
        {
            Header = L.T("Direction"),
            ItemsSource = new[]
            {
                PalettePanel.Item(L.T("Forward"), () => w.SetTagDirection(t, LoopDir.Forward)),
                PalettePanel.Item(L.T("Reverse"), () => w.SetTagDirection(t, LoopDir.Reverse)),
                PalettePanel.Item(L.T("Ping-pong"), () => w.SetTagDirection(t, LoopDir.PingPong)),
            },
        };
        var colors = new List<object>();
        foreach (var c in MainWindow.TagColors)
        {
            var color = c;
            colors.Add(L.Keep(PalettePanel.Item(MainWindow.Hex(c), () => w.SetTagColor(t, color))));
            ((MenuItem)colors[^1]).Icon = new Border { Width = 14, Height = 14, Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)) };
        }
        colors.Add(PalettePanel.Item(L.T("Primary colour"), () => w.SetTagColor(t, w.Tools.Primary)));
        return new ContextMenu
        {
            ItemsSource = new object[]
            {
                PalettePanel.Item(L.T("Rename tag…"), () => _ = w.RenameTagAsync(t)),
                dir,
                new MenuItem { Header = L.T("Colour"), ItemsSource = colors },
                PalettePanel.Item(L.T("Use the picked frames"), () => w.SetTagRange(t)),
                new Separator(),
                PalettePanel.Item(L.T("Remove tag"), () => w.RemoveTag(t)),
            },
        };
    }
}
