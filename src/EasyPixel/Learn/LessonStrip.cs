using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EasyPixel.Core.Learn;

namespace EasyPixel.Learn;

/// <summary>
/// The bar above the canvas while an exercise is open: the goal, the self-check items, Check (with its results),
/// Back to lesson and a close button.
/// </summary>
public sealed class LessonStrip : Border
{
    private readonly StackPanel _body = new() { Spacing = 6 };
    private readonly StackPanel _results = new() { Spacing = 2 };
    private readonly WrapPanel _checks = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _title = new() { FontWeight = FontWeight.SemiBold, FontSize = 14, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _goal = L.Keep(new TextBlock { TextWrapping = TextWrapping.Wrap });
    private readonly TextBlock _summary = new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

    public LessonStrip()
    {
        Classes.Add("bar");
        BorderThickness = new Thickness(0, 0, 0, 1);
        Padding = new Thickness(12, 8);
        IsVisible = false;

        CheckButton = new Button { Content = L.T("Check"), Background = Res.Brush("Accent"), Foreground = Res.Brush("AccentText"), Padding = new Thickness(18, 5), HorizontalContentAlignment = HorizontalAlignment.Center };
        BackButton = new Button { Content = L.T("Back to lesson"), HorizontalContentAlignment = HorizontalAlignment.Center };
        CloseButton = new Button { Content = "✕", Padding = new Thickness(10, 5) };
        ToolTip.SetTip(CloseButton, L.T("Close the lesson bar"));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(12, 0, 0, 0) };
        buttons.Children.Add(CheckButton);
        buttons.Children.Add(BackButton);
        buttons.Children.Add(CloseButton);
        DockPanel.SetDock(buttons, Dock.Right);

        _body.Children.Add(_title);
        _body.Children.Add(_goal);
        _body.Children.Add(_checks);
        _body.Children.Add(_summary);
        _body.Children.Add(_results);
        _summary.IsVisible = false;
        var dock = new DockPanel();
        dock.Children.Add(buttons);
        dock.Children.Add(new ScrollViewer { Content = _body, MaxHeight = 230 });
        Child = dock;
    }

    public Button CheckButton { get; }
    public Button BackButton { get; }
    public Button CloseButton { get; }

    /// <summary>The self-check boxes, in the lesson's order.</summary>
    internal IReadOnlyList<CheckBox> SelfChecks => _checks.Children.OfType<CheckBox>().ToList();

    /// <summary>The lines of the last check as shown (translated), and its verdict.</summary>
    internal IReadOnlyList<string> ResultLines => _results.Children.OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
    internal string? Summary => _summary.IsVisible ? _summary.Text : null;

    /// <summary>Fills the bar for a lesson's exercise and shows it.</summary>
    internal void Show(Lesson lesson, TryItBlock tryIt)
    {
        _title.Text = L.F("Practice: {0}", lesson.Title);
        _goal.Text = Inline.Plain(string.Join(" ", tryIt.Goal));
        _checks.Children.Clear();
        foreach (var item in tryIt.SelfChecks)
            _checks.Children.Add(L.Keep(new CheckBox { Content = Inline.Plain(item), Margin = new Thickness(0, 0, 16, 0) }));
        ClearResult();
        IsVisible = true;
    }

    internal void ClearResult()
    {
        _results.Children.Clear();
        _summary.IsVisible = false;
    }

    /// <summary>Shows what Check found; true when the exercise passed (every rule and every self-check).</summary>
    internal bool ShowResult(CheckResult result)
    {
        bool ticked = SelfChecks.All(c => c.IsChecked == true);
        bool passed = result.Passed && ticked;
        _results.Children.Clear();
        foreach (var line in result.Lines)
            _results.Children.Add(new TextBlock { Text = (line.Ok ? "✓ " : "✗ ") + L.F(line.Text, line.Args), TextWrapping = TextWrapping.Wrap });
        _summary.Text = passed ? L.T("Passed! Well done: this lesson is marked as passed.")
            : result.Passed ? L.T("The automatic checks pass. Tick the self-check items that are true for your drawing, then press Check again.")
            : L.T("Not yet: fix the points marked ✗ and press Check again.");
        _summary.IsVisible = true;
        return passed;
    }
}
