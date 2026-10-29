using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Learn;

namespace EasyPixel.Learn;

/// <summary>
/// Help → Learn pixel art: the lesson list (Fundamentals / Styles, with read and passed marks) and the lesson page
/// (text, zoomable illustrations, the "Try it" box that opens the exercise in the editor).
/// </summary>
public sealed class LearnWindow : Window
{
    private static readonly double[] Scales = { 0.5, 0.75, 1, 1.5, 2, 3 };

    private readonly MainWindow _main;
    private readonly StackPanel _list = new() { Spacing = 2, Margin = new Thickness(8) };
    private readonly StackPanel _page = new() { Spacing = 12, Margin = new Thickness(28, 20, 28, 28), MaxWidth = 780, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ScrollViewer _scroll;
    private readonly TextBlock _zoomText = new() { VerticalAlignment = VerticalAlignment.Center, MinWidth = 60, TextAlignment = TextAlignment.Center };
    private readonly Dictionary<string, Button> _buttons = new();
    private readonly List<ArtView> _art = new();
    private int _scale = Array.IndexOf(Scales, 1.0);

    public LearnWindow(MainWindow main)
    {
        _main = main;
        Title = L.T("Learn pixel art");
        Icon = main.Icon;
        Width = 1100;
        Height = 760;
        MinWidth = 720;
        MinHeight = 480;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var side = new DockPanel();
        var sideTitle = new TextBlock { Text = L.T("Lessons"), FontSize = 16, FontWeight = FontWeight.SemiBold, Margin = new Thickness(12, 12, 12, 4) };
        DockPanel.SetDock(sideTitle, Dock.Top);
        side.Children.Add(sideTitle);
        side.Children.Add(new ScrollViewer { Content = _list });
        var sideBorder = new Border { Classes = { "bar" }, BorderThickness = new Thickness(0, 0, 1, 0), Width = 280, Child = side };
        DockPanel.SetDock(sideBorder, Dock.Left);

        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(12, 6) };
        bar.Children.Add(new TextBlock { Text = L.T("Pictures"), VerticalAlignment = VerticalAlignment.Center });
        bar.Children.Add(SmallButton("−", L.T("Smaller pictures"), () => SetScale(_scale - 1)));
        bar.Children.Add(_zoomText);
        bar.Children.Add(SmallButton("+", L.T("Bigger pictures"), () => SetScale(_scale + 1)));
        var barBorder = new Border { Classes = { "bar" }, BorderThickness = new Thickness(0, 0, 0, 1), Child = bar };
        DockPanel.SetDock(barBorder, Dock.Top);

        _scroll = new ScrollViewer { Content = _page };
        var root = new DockPanel();
        root.Children.Add(sideBorder);
        root.Children.Add(barBorder);
        root.Children.Add(_scroll);
        Content = root;

        BuildList();
        SetScale(_scale);
        var first = App.Settings.LessonsRead.Count == 0 ? LessonLibrary.Ids.FirstOrDefault() : NextUnpassed();
        if (first != null) ShowLesson(first);
    }

    protected override Type StyleKeyOverride => typeof(Window);

    /// <summary>The lesson on screen.</summary>
    internal LessonLoad? Current { get; private set; }

    internal IReadOnlyDictionary<string, Button> LessonButtons => _buttons;
    internal Button? TryItButton { get; private set; }
    internal TextBlock? FallbackNote { get; private set; }
    internal IReadOnlyList<ArtView> Pictures => _art;
    internal double Scale => Scales[_scale];

    private static string? NextUnpassed() =>
        LessonLibrary.Ids.FirstOrDefault(id => !App.Settings.LessonsPassed.Contains(id)) ?? LessonLibrary.Ids.FirstOrDefault();

    private static Button SmallButton(string text, string tip, Action click)
    {
        var b = new Button { Content = text, Padding = new Thickness(10, 2), MinWidth = 32, HorizontalContentAlignment = HorizontalAlignment.Center };
        ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        return b;
    }

    // ---- the list ----

    private void BuildList()
    {
        _list.Children.Clear();
        _buttons.Clear();
        var all = LessonLibrary.All(L.Language);
        foreach (var group in new[] { LessonGroup.Fundamentals, LessonGroup.Styles })
        {
            var lessons = all.Where(l => l.Lesson.Group == group).ToList();
            if (lessons.Count == 0) continue;
            _list.Children.Add(new TextBlock
            {
                Text = group == LessonGroup.Fundamentals ? L.T("Fundamentals") : L.T("Styles"),
                Classes = { "dim" }, FontWeight = FontWeight.SemiBold, Margin = new Thickness(4, 10, 4, 2),
            });
            foreach (var l in lessons) _list.Children.Add(_buttons[l.Lesson.Id] = LessonButton(l.Lesson));
        }
        RefreshMarks();
    }

    private Button LessonButton(Lesson lesson)
    {
        var mark = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Tag = "mark" };
        DockPanel.SetDock(mark, Dock.Right);
        var title = L.Keep(new TextBlock { Text = $"{lesson.Number}. {lesson.Title}", TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
        var row = new DockPanel();
        row.Children.Add(mark);
        row.Children.Add(title);
        var b = new Button
        {
            Content = row, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent, Padding = new Thickness(8, 5), Tag = lesson.Id,
        };
        b.Click += (_, _) => ShowLesson(lesson.Id);
        return b;
    }

    /// <summary>Read (✓) and passed (★) marks, and the highlight on the open lesson.</summary>
    internal void RefreshMarks()
    {
        var s = App.Settings;
        foreach (var (id, b) in _buttons)
        {
            bool current = Current?.Lesson.Id == id;
            b.Background = current ? Res.Brush("Accent") : Brushes.Transparent;
            b.Foreground = current ? Res.Brush("AccentText") : Res.Brush("Text");
            var mark = ((DockPanel)b.Content!).Children.OfType<TextBlock>().First();
            bool passed = s.LessonsPassed.Contains(id), read = s.LessonsRead.Contains(id);
            mark.Text = passed ? "★" : read ? "✓" : "";
            ToolTip.SetTip(b, passed ? L.T("Exercise passed") : read ? L.T("Read") : null);
        }
    }

    // ---- the page ----

    /// <summary>Shows a lesson (in the interface language, English when it isn't translated) and marks it read.</summary>
    internal void ShowLesson(string id)
    {
        Current = LessonLibrary.Load(id, L.Language);
        var s = App.Settings;
        if (!s.LessonsRead.Contains(id))
        {
            s.LessonsRead.Add(id);
            s.Save();
        }
        BuildPage();
        RefreshMarks();
        _scroll.Offset = default;
    }

    private void BuildPage()
    {
        _page.Children.Clear();
        _art.Clear();
        TryItButton = null;
        FallbackNote = null;
        if (Current is not { } load) return;
        var lesson = load.Lesson;
        _page.Children.Add(L.Keep(new TextBlock { Text = lesson.Title, FontSize = 26, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap }));
        if (load.IsFallback)
            _page.Children.Add(FallbackNote = new TextBlock { Text = L.T("This lesson isn't translated yet — showing English."), Classes = { "dim" }, FontStyle = FontStyle.Italic, TextWrapping = TextWrapping.Wrap });
        if (lesson.Summary.Length > 0)
            _page.Children.Add(L.Keep(new TextBlock { Text = lesson.Summary, Classes = { "dim" }, FontSize = 15, TextWrapping = TextWrapping.Wrap }));

        foreach (var block in lesson.Blocks) _page.Children.Add(Render(block));
        _page.Children.Add(NavRow(lesson.Id));
    }

    private Control Render(LessonBlock block) => block switch
    {
        HeadingBlock h => L.Keep(new TextBlock { Text = h.Text, FontSize = 19, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 14, 0, 0), TextWrapping = TextWrapping.Wrap }),
        ParagraphBlock p => Text(p.Text),
        ListBlock l => List(l),
        TipBlock t => Box(L.T("Tip"), Res.Brush("Accent"), Text(t.Text)),
        MistakeBlock m => Box(L.T("Common mistake"), Res.Brush("Danger"), Text(m.Text)),
        ExampleBlock e => Examples(e),
        TryItBlock t => TryIt(t),
        _ => new Control(),
    };

    /// <summary>Lesson text with **bold** runs.</summary>
    internal static TextBlock Text(string text, double size = 14)
    {
        var tb = L.Keep(new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = size, LineHeight = size * 1.5 });
        foreach (var (run, bold) in Core.Learn.Inline.Split(text))
            tb.Inlines!.Add(new Run(run) { FontWeight = bold ? FontWeight.Bold : FontWeight.Normal });
        return tb;
    }

    private static Control List(ListBlock l)
    {
        var panel = new StackPanel { Spacing = 4 };
        for (int i = 0; i < l.Items.Count; i++)
        {
            var bullet = new TextBlock { Text = l.Ordered ? $"{i + 1}." : "•", Width = 24, FontSize = 14, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 8, 0) };
            DockPanel.SetDock(bullet, Dock.Left);
            var row = new DockPanel();
            row.Children.Add(bullet);
            row.Children.Add(Text(l.Items[i]));
            panel.Children.Add(row);
        }
        return panel;
    }

    private static Control Box(string label, IBrush accent, Control body)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(body);
        return new Border
        {
            Background = Res.Brush("Bg2"), BorderBrush = accent, BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(14, 10), CornerRadius = new CornerRadius(4), Child = panel,
        };
    }

    private Control Examples(ExampleBlock block)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var ex in block.Examples)
        {
            var cell = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 20, 4) };
            if (LessonArt.Exists(ex.Id))
            {
                var art = LessonArt.Build(ex.Id);
                var view = new ArtView(art, ZoomFor(art));
                _art.Add(view);
                cell.Children.Add(view);
            }
            if (ex.Caption.Length > 0)
            {
                var caption = Text(ex.Caption, 13);
                caption.Classes.Add("dim");
                caption.MaxWidth = 340;
                caption.HorizontalAlignment = HorizontalAlignment.Left;
                cell.Children.Add(caption);
            }
            wrap.Children.Add(cell);
        }
        return wrap;
    }

    private int ZoomFor(ArtExample art) => Math.Max(1, (int)Math.Round(art.Zoom * Scales[_scale]));

    private Control TryIt(TryItBlock t)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = L.T("Practice"), FontSize = 16, FontWeight = FontWeight.SemiBold });
        foreach (var g in t.Goal) panel.Children.Add(Text(g));
        if (t.SelfChecks.Count > 0)
        {
            panel.Children.Add(new TextBlock { Text = L.T("When you press Check, you'll also tick these yourself:"), Classes = { "dim" }, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(List(new ListBlock(t.SelfChecks)));
        }
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        TryItButton = new Button { Content = L.T("Try it in the editor"), Padding = new Thickness(16, 6) };
        TryItButton.Background = Res.Brush("Accent");
        TryItButton.Foreground = Res.Brush("AccentText");
        var id = Current!.Lesson.Id;
        TryItButton.Click += (_, _) => _ = _main.StartPracticeAsync(id);
        row.Children.Add(TryItButton);
        if (App.Settings.LessonsPassed.Contains(id))
            row.Children.Add(new TextBlock { Text = L.T("★ Exercise passed"), VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(row);
        return new Border
        {
            Background = Res.Brush("Bg2"), BorderBrush = Res.Brush("Accent"), BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 12), CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 4, 0, 0), Child = panel,
        };
    }

    private Control NavRow(string id)
    {
        var ids = LessonLibrary.Ids;
        int i = ids.ToList().IndexOf(id);
        var row = new DockPanel { Margin = new Thickness(0, 20, 0, 0) };
        if (i > 0)
        {
            var prev = new Button { Content = L.T("← Previous lesson") };
            prev.Click += (_, _) => ShowLesson(ids[i - 1]);
            DockPanel.SetDock(prev, Dock.Left);
            row.Children.Add(prev);
        }
        if (i >= 0 && i < ids.Count - 1)
        {
            var next = new Button { Content = L.T("Next lesson →") };
            next.Click += (_, _) => ShowLesson(ids[i + 1]);
            DockPanel.SetDock(next, Dock.Right);
            row.Children.Add(next);
        }
        row.Children.Add(new Control());
        return row;
    }

    /// <summary>Pictures smaller or bigger (every illustration keeps an integer zoom, so it stays crisp).</summary>
    internal void SetScale(int index)
    {
        _scale = Math.Clamp(index, 0, Scales.Length - 1);
        _zoomText.Text = $"{Scales[_scale] * 100:0}%";
        foreach (var v in _art) v.Zoom = ZoomFor(v.Art);
    }
}
