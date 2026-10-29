using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

/// <summary>
/// Checks over every text the main window shows: each one comes from the translation table, each one is readable
/// (contrast at least 4.5:1) in both themes. Also saves a picture of the window per theme to %TEMP%\easypixel_snap_*.png.
/// </summary>
public class UiAuditTests
{
    /// <summary>A window showing every panel with content: layers in a group, frames, a tag, a palette.</summary>
    private static MainWindow FullWindow()
    {
        var w = new MainWindow { Width = 1280, Height = 800 };
        w.Dialogs = new FakeDialogs();
        w.Show();
        var s = MainWindow.NewDocument(32, 32, ColorMode.Rgba);
        s.Frames.Add(new Frame());
        s.Frames.Add(new Frame());
        var ink = new Layer { Name = "Ink" };
        LayerTree.Insert(s, ink, s.Layers[0]);
        LayerTree.Group(s, ink, "Body");
        s.Tags.Add(new Tag { Name = "walk", From = 0, To = 1, Color = MainWindow.TagColors[0] });
        s.Tags.Add(new Tag { Name = "idle", From = 2, To = 2, Color = MainWindow.TagColors[3] });
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        for (int i = 0; i < 64; i++) cel.Image.Pixels[i * 7] = PanelsColor(i);
        w.SetSession(new EditorSession(s));
        w.Canvas.FitToView();
        Harness.Pump();
        return w;
    }

    private static uint PanelsColor(int i) => new Rgba((byte)(i * 4), (byte)(255 - i * 3), (byte)(i * 2), 255).Packed;

    private static IEnumerable<StyledElement> Everything(StyledElement root)
    {
        var seen = new HashSet<StyledElement>();
        var stack = new Stack<StyledElement>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            if (!seen.Add(e)) continue;
            yield return e;
            if (e is Visual v) foreach (var c in v.GetVisualChildren()) stack.Push((StyledElement)c);
            if (e is ILogical l) foreach (var c in l.LogicalChildren) if (c is StyledElement se) stack.Push(se);
            if (e is Control ctl && ctl.ContextMenu is { } cm) stack.Push(cm);
            if (e is ItemsControl ic) foreach (var item in ic.Items) if (item is StyledElement se) stack.Push(se);
        }
    }

    private static bool Kept(StyledElement e)
    {
        for (StyledElement? p = e; p != null; p = p.Parent ?? (p as Visual)?.GetVisualParent() as StyledElement)
            if (p.Classes.Contains(L.KeepClass)) return true;
        return false;
    }

    /// <summary>Every string a person can read on the element, with where it comes from.</summary>
    private static IEnumerable<(string Text, string Where)> Texts(StyledElement e)
    {
        if (e is TextBlock tb && !string.IsNullOrEmpty(tb.Text)) yield return (tb.Text, "TextBlock");
        if (e is Avalonia.Controls.Primitives.HeaderedSelectingItemsControl { Header: string h }) yield return (h, "Header");
        if (e is MenuItem { Header: string mh }) yield return (mh, "MenuItem");
        if (e is ContentControl { Content: string cs }) yield return (cs, e.GetType().Name);
        if (e is Control c && ToolTip.GetTip(c) is string tip) yield return (tip, "ToolTip of " + e.GetType().Name);
        if (e is ItemsControl ic) foreach (var item in ic.Items) if (item is string s) yield return (s, "item of " + e.GetType().Name);
        if (e is Window win && !string.IsNullOrEmpty(win.Title)) yield return (win.Title, "Title");
    }

    [AvaloniaFact]
    public void AllTexts_GoThroughLoc()
    {
        // Every English text maps to "⟦text⟧": anything with letters on screen without the brackets skipped L.T.
        var en = JsonSerializer.Deserialize<Dictionary<string, string>>(L.ReadEmbedded("en")!)!;
        var marked = en.Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => "⟦" + kv.Value + "⟧");
        L.UseTable(marked, "test");
        MainWindow? w = null;
        try
        {
            w = FullWindow();
            w.Tools.Current = Tools.ToolKind.Bucket; // an options bar with fields
            Harness.Pump();
            var bad = new List<string>();
            int seen = 0;
            foreach (var e in Everything(w))
            {
                if (Kept(e)) continue;
                foreach (var (text, where) in Texts(e))
                {
                    if (!text.Any(char.IsLetter)) continue;
                    seen++;
                    if (!text.Contains('⟦')) bad.Add($"{where}: \"{text}\"");
                }
            }
            Assert.True(seen > 150, $"only {seen} texts found");
            Assert.True(bad.Count == 0, "Texts that don't go through L.T:\n" + string.Join("\n", bad.Distinct()));
        }
        finally
        {
            L.Load("en");
            if (w != null) { w.Session.History.MarkSaved(); w.Close(); }
        }
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Contrast_AllTexts_AtLeast4_5(string theme)
    {
        var app = (App)Application.Current!;
        app.ApplyTheme(theme == "light");
        MainWindow? w = null;
        try
        {
            w = FullWindow();
            w.Tools.Current = Tools.ToolKind.Bucket;
            Harness.Pump();
            var frame = w.CaptureRenderedFrame();
            var path = Path.Combine(Path.GetTempPath(), $"easypixel_snap_{theme}.png");
            frame?.Save(path);
            Assert.True(File.Exists(path));

            var bad = new List<string>();
            int checkedTexts = 0;
            foreach (var tb in w.GetVisualDescendants().OfType<TextBlock>())
            {
                if (string.IsNullOrWhiteSpace(tb.Text) || !tb.IsEffectivelyVisible || !tb.IsEffectivelyEnabled || tb.Bounds.Width <= 0) continue;
                if (tb.Foreground is not ISolidColorBrush fg) continue;
                var bg = Background(tb);
                var text = Contrast.Over(fg.Color, fg.Opacity * tb.Opacity, bg);
                double ratio = Contrast.Ratio(text, bg);
                checkedTexts++;
                if (ratio < 4.5) bad.Add($"\"{tb.Text}\" {text} on {bg}: {ratio:0.00}");
            }
            Assert.True(checkedTexts > 40, $"only {checkedTexts} texts measured");
            Assert.True(bad.Count == 0, $"Texts under 4.5:1 ({theme}):\n" + string.Join("\n", bad.Distinct()));
        }
        finally
        {
            if (w != null) { w.Session.History.MarkSaved(); w.Close(); }
            app.ApplyTheme(false);
        }
    }

    /// <summary>
    /// Learn pixel art on screen: the main window practising an exercise (lesson bar with a check result) and the Learn
    /// window on a lesson with every kind of block.
    /// </summary>
    private static (MainWindow Main, Learn.LearnWindow Learn) LearnScene()
    {
        var w = FullWindow();
        var lesson = Core.Learn.LessonLibrary.Ids.First(i => i.StartsWith("05-"));
        Assert.True(w.StartPracticeAsync(lesson).Result);
        w.CheckPractice();
        var learn = w.ShowLearn(lesson);
        Harness.Pump();
        return (w, learn);
    }

    [AvaloniaFact]
    public void Learn_AllTexts_GoThroughLoc()
    {
        var en = JsonSerializer.Deserialize<Dictionary<string, string>>(L.ReadEmbedded("en")!)!;
        L.UseTable(en.Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => "⟦" + kv.Value + "⟧"), "test");
        MainWindow? w = null;
        try
        {
            var (main, learn) = LearnScene();
            w = main;
            var bad = new List<string>();
            int seen = 0;
            foreach (var root in new StyledElement[] { learn, main.LessonBar })
                foreach (var e in Everything(root))
                {
                    if (Kept(e)) continue;
                    foreach (var (text, where) in Texts(e))
                    {
                        if (!text.Any(char.IsLetter)) continue;
                        seen++;
                        if (!text.Contains('⟦')) bad.Add($"{where}: \"{text}\"");
                    }
                }
            Assert.True(seen > 15, $"only {seen} texts found");
            Assert.True(bad.Count == 0, "Texts that don't go through L.T:\n" + string.Join("\n", bad.Distinct()));
            learn.Close();
        }
        finally
        {
            L.Load("en");
            if (w != null) { w.Session.History.MarkSaved(); w.Close(); }
        }
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Learn_Contrast_AtLeast4_5(string theme)
    {
        var app = (App)Application.Current!;
        app.ApplyTheme(theme == "light");
        MainWindow? w = null;
        try
        {
            var (main, learn) = LearnScene();
            w = main;
            learn.CaptureRenderedFrame()?.Save(Path.Combine(Path.GetTempPath(), $"easypixel_snap_learn_{theme}.png"));
            main.CaptureRenderedFrame()?.Save(Path.Combine(Path.GetTempPath(), $"easypixel_snap_lessonbar_{theme}.png"));
            var bad = new List<string>();
            int checkedTexts = 0;
            foreach (var root in new Visual[] { learn, main.LessonBar })
                foreach (var tb in root.GetVisualDescendants().OfType<TextBlock>())
                {
                    if (string.IsNullOrWhiteSpace(tb.Text) || !tb.IsEffectivelyVisible || !tb.IsEffectivelyEnabled || tb.Bounds.Width <= 0) continue;
                    if (tb.Foreground is not ISolidColorBrush fg) continue;
                    var bg = Background(tb);
                    var text = Contrast.Over(fg.Color, fg.Opacity * tb.Opacity, bg);
                    double ratio = Contrast.Ratio(text, bg);
                    checkedTexts++;
                    if (ratio < 4.5) bad.Add($"\"{tb.Text}\" {text} on {bg}: {ratio:0.00}");
                }
            Assert.True(checkedTexts > 30, $"only {checkedTexts} texts measured");
            Assert.True(bad.Count == 0, $"Texts under 4.5:1 ({theme}):\n" + string.Join("\n", bad.Distinct()));
            learn.Close();
        }
        finally
        {
            if (w != null) { w.Session.History.MarkSaved(); w.Close(); }
            app.ApplyTheme(false);
        }
    }

    /// <summary>The colour behind a visual: the backgrounds of its ancestors, the farthest opaque one first, blended down.</summary>
    private static Color Background(Visual v)
    {
        var layers = new List<(IBrush Brush, double Opacity)>();
        for (var p = v; p != null; p = p.GetVisualParent())
        {
            var b = p switch
            {
                Border bd => bd.Background,
                Panel pn => pn.Background,
                ContentPresenter cp => cp.Background,
                TextBlock t when t != v => t.Background,
                TopLevel tl => tl.Background,
                _ => null,
            };
            if (b is ISolidColorBrush s && s.Color.A > 0)
            {
                layers.Add((b, p.Opacity));
                if (s.Color.A == 255 && s.Opacity >= 1) break;
            }
        }
        var color = Colors.White;
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var s = (ISolidColorBrush)layers[i].Brush;
            color = Contrast.Over(s.Color, s.Opacity * layers[i].Opacity, color);
        }
        return color;
    }
}
