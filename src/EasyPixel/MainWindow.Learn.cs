using Avalonia.Interactivity;
using EasyPixel.Core.Learn;
using EasyPixel.Learn;

namespace EasyPixel;

// Help → Learn pixel art: the lesson window, and the practice document with its lesson bar above the canvas.
public partial class MainWindow
{
    /// <summary>The open exercise: its lesson, the exercise and the session holding its document.</summary>
    internal sealed record Practice(Lesson Lesson, TryItBlock TryIt, Exercise Exercise, EditorSession Session);

    private LearnWindow? _learn;

    /// <summary>The exercise being practised, or null.</summary>
    internal Practice? CurrentPractice { get; private set; }

    private void Learn_Click(object? sender, RoutedEventArgs e) => ShowLearn();

    /// <summary>Opens (or brings back) the Learn window, on <paramref name="lessonId"/> when given.</summary>
    internal LearnWindow ShowLearn(string? lessonId = null)
    {
        if (_learn == null)
        {
            _learn = new LearnWindow(this);
            _learn.Closed += (_, _) => _learn = null;
            _learn.Show(this);
        }
        else _learn.Activate();
        if (lessonId != null && _learn.Current?.Lesson.Id != lessonId) _learn.ShowLesson(lessonId);
        return _learn;
    }

    /// <summary>"Try it": opens the lesson's exercise as a new document (asking to save the current one first) with the lesson bar.</summary>
    internal async Task<bool> StartPracticeAsync(string lessonId)
    {
        var lesson = LessonLibrary.Load(lessonId, L.Language).Lesson;
        if (lesson.TryIt is not { } tryIt || !Exercises.Exists(tryIt.ExerciseId)) return false;
        if (!await ConfirmDiscardAsync()) return false;
        var exercise = Exercises.Get(tryIt.ExerciseId);
        var session = new EditorSession(exercise.CreateDocument());
        SetSession(session);
        CurrentPractice = new Practice(lesson, tryIt, exercise, session);
        LessonBar.Show(lesson, tryIt);
        Canvas.FitToView();
        // Fit again once the lesson bar has taken its height.
        Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (Session == session) Canvas.FitToView(); }, Avalonia.Threading.DispatcherPriority.Background);
        _learn?.Close();
        ShowStatus(L.F("Practice: {0}", lesson.Title));
        return true;
    }

    /// <summary>Check: judges the practice drawing, shows the result and records a pass. Null when no exercise is open.</summary>
    internal bool? CheckPractice()
    {
        if (CurrentPractice is not { } p || Session != p.Session) return null;
        Session.DropFloating();
        bool passed = LessonBar.ShowResult(p.Exercise.Check(Session.Doc));
        if (passed && !App.Settings.LessonsPassed.Contains(p.Lesson.Id))
        {
            App.Settings.LessonsPassed.Add(p.Lesson.Id);
            App.Settings.Save();
        }
        return passed;
    }

    /// <summary>Back to lesson: the Learn window on the practice's lesson (the practice stays open).</summary>
    internal void BackToLesson()
    {
        if (CurrentPractice is { } p) ShowLearn(p.Lesson.Id);
        else ShowLearn();
    }

    /// <summary>Hides the lesson bar; the drawing stays as an ordinary document.</summary>
    internal void EndPractice()
    {
        CurrentPractice = null;
        LessonBar.IsVisible = false;
    }

    private void AttachLessonStrip()
    {
        LessonBar.CheckButton.Click += (_, _) => CheckPractice();
        LessonBar.BackButton.Click += (_, _) => BackToLesson();
        LessonBar.CloseButton.Click += (_, _) => EndPractice();
    }
}
