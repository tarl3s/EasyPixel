using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using EasyPixel.Core.Learn;
using EasyPixel.Learn;

namespace EasyPixel.UiTests;

public class LearnUiTests
{
    private static string FirstSprite => LessonLibrary.Ids.First(i => i.StartsWith("01-"));
    private static string Lines => LessonLibrary.Ids.First(i => i.StartsWith("02-"));

    private static void Click(Button b)
    {
        b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
    }

    [AvaloniaFact]
    public void HelpMenu_OpensLearnPixelArt()
    {
        using var h = new Harness();
        var help = h.Window.MainMenu.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Help");
        var learn = help.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Learn pixel art");
        learn.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Harness.Pump();
        var w = h.Window.ShowLearn(); // the same window, brought back
        Assert.True(w.IsVisible);
        Assert.Same(w, h.Window.ShowLearn());
        w.Close();
    }

    [AvaloniaFact]
    public void LearnWindow_ListsEveryLesson_ShowsOne_AndRemembersReading()
    {
        using var h = new Harness();
        App.Settings.LessonsRead.Clear();
        var w = h.Window.ShowLearn();
        Harness.Pump();
        Assert.Equal(LessonLibrary.Ids.Count, w.LessonButtons.Count);
        Assert.Equal(LessonLibrary.Ids[0], w.Current!.Lesson.Id); // nothing read yet: the first lesson
        Assert.Null(w.FallbackNote);

        Click(w.LessonButtons[Lines]);
        Assert.Equal(Lines, w.Current!.Lesson.Id);
        Assert.Contains(Lines, App.Settings.LessonsRead);
        Assert.Contains(Lines, Settings.Load().LessonsRead); // saved
        Assert.NotEmpty(w.Pictures);
        Assert.NotNull(w.TryItButton);

        int before = w.Pictures[0].Zoom;
        w.SetScale(5); // 300 %
        Assert.True(w.Pictures[0].Zoom > before);
        w.SetScale(0); // 50 %, still at least 1×
        Assert.All(w.Pictures, p => Assert.True(p.Zoom >= 1));
        w.Close();
    }

    [AvaloniaFact]
    public void AnimatedIllustration_StepsThroughItsFrames()
    {
        var view = new ArtView(LessonArt.Build("walk-4frame"), 4);
        Assert.True(view.Animated);
        view.Step();
        view.Step();
        Assert.Equal(2, view.Frame);
        Assert.Equal(14 * 4, view.Child!.Width);
    }

    [AvaloniaFact]
    public void UntranslatedLesson_ShowsTheEnglishNote()
    {
        using var h = new Harness();
        try
        {
            L.Load("de");
            var w = h.Window.ShowLearn(FirstSprite);
            Assert.Equal(!LessonLibrary.HasTranslation(FirstSprite, "de"), w.FallbackNote != null);
            w.Close();
        }
        finally { L.Load("en"); }
    }

    /// <summary>Try it → the exercise opens with the lesson bar; Check fails, then passes once drawn and ticked; the pass is saved.</summary>
    [AvaloniaFact]
    public void TryIt_Check_BackToLesson()
    {
        using var h = new Harness();
        App.Settings.LessonsPassed.Remove(FirstSprite);
        var w = h.Window.ShowLearn(FirstSprite);
        Click(w.TryItButton!);

        var p = h.Window.CurrentPractice;
        Assert.NotNull(p);
        Assert.Equal("first-sprite", p!.Exercise.Id);
        Assert.Same(p.Session, h.Session);
        Assert.True(h.Window.LessonBar.IsVisible);
        Assert.False(w.IsVisible); // the lesson window steps aside
        Assert.Equal(2, h.Doc.Layers.Count);
        Assert.Equal(Exercise.WorkLayerName, h.Session.Layer.Name); // drawing goes on the work layer
        Assert.Equal(p.TryIt.SelfChecks.Count, h.Window.LessonBar.SelfChecks.Count);

        Assert.False(h.Window.CheckPractice());
        Assert.Equal(p.Exercise.Rules.Count, h.Window.LessonBar.ResultLines.Count);
        Assert.Contains(h.Window.LessonBar.ResultLines, l => l.StartsWith("✗"));

        // Draw: a stroke with the pencil, then fill a body of 4 colours on the drawing layer.
        h.Window.Tools.Current = Tools.ToolKind.Pencil;
        h.Drag((4, 4), (11, 4));
        var cel = h.Doc.Cels[(h.Session.Layer, 0)];
        var c = new ArtCanvas(16, 16);
        cel.Image.Pixels.CopyTo(c.Image.Pixels, 0);
        c.Ellipse(3, 3, 12, 12, Pal.Red).Ellipse(5, 5, 7, 7, Pal.Orange).Outline(Pal.Black);
        c.Image.Pixels.CopyTo(cel.Image.Pixels, 0);

        Assert.False(h.Window.CheckPractice()); // the rules pass, the self-checks aren't ticked
        Assert.All(h.Window.LessonBar.ResultLines, l => Assert.StartsWith("✓", l));
        Assert.Contains("Tick", h.Window.LessonBar.Summary);
        Assert.DoesNotContain(FirstSprite, App.Settings.LessonsPassed);

        foreach (var box in h.Window.LessonBar.SelfChecks) box.IsChecked = true;
        Click(h.Window.LessonBar.CheckButton);
        Assert.Contains("Passed", h.Window.LessonBar.Summary);
        Assert.Contains(FirstSprite, App.Settings.LessonsPassed);
        Assert.Contains(FirstSprite, Settings.Load().LessonsPassed);

        Click(h.Window.LessonBar.BackButton);
        var back = h.Window.ShowLearn();
        Assert.Equal(FirstSprite, back.Current!.Lesson.Id);
        Assert.Equal("★", ((DockPanel)back.LessonButtons[FirstSprite].Content!).Children.OfType<TextBlock>().First().Text);
        Assert.NotNull(h.Window.CurrentPractice); // the practice stays open behind
        back.Close();

        Click(h.Window.LessonBar.CloseButton);
        Assert.Null(h.Window.CurrentPractice);
        Assert.False(h.Window.LessonBar.IsVisible);
        Assert.Null(h.Window.CheckPractice());
    }

    [AvaloniaFact]
    public void AnotherDocument_EndsThePractice()
    {
        using var h = new Harness();
        Assert.True(h.Window.StartPracticeAsync(Lines).Result);
        Assert.True(h.Window.LessonBar.IsVisible);
        h.Window.SetSession(new EditorSession(new Core.Model.Sprite(8, 8)));
        Assert.Null(h.Window.CurrentPractice);
        Assert.False(h.Window.LessonBar.IsVisible);
    }

    [AvaloniaFact]
    public void TryIt_AsksBeforeDroppingUnsavedWork()
    {
        using var h = new Harness();
        h.Window.Tools.Current = Tools.ToolKind.Pencil;
        h.Drag((1, 1), (3, 1));
        Assert.True(h.Session.History.IsDirty);
        h.Dialogs.SaveChoices.Enqueue(Dialogs.SaveChoice.Cancel);
        Assert.False(h.Window.StartPracticeAsync(Lines).Result);
        Assert.Null(h.Window.CurrentPractice);
    }
}
