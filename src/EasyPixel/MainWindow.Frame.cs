using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Panels;

namespace EasyPixel;

// Frame menu and the timeline's commands: frames, durations, tags, playback, onion skin, preview.
public partial class MainWindow
{
    /// <summary>Colours offered for tags (Aseprite-like), and the one a new tag gets.</summary>
    internal static readonly Rgba[] TagColors =
    {
        new(0x5B, 0x8D, 0xEF, 255), new(0xE0, 0x4F, 0x5F, 255), new(0xF2, 0xA6, 0x3B, 255), new(0xF5, 0xDC, 0x4C, 255),
        new(0x4F, 0xC0, 0x6A, 255), new(0x3F, 0xC6, 0xC6, 255), new(0x9B, 0x6B, 0xE0, 255), new(0x8A, 0x8F, 0x99, 255),
    };

    private readonly Playback _playback = new();
    private DispatcherTimer? _playTimer;

    /// <summary>The frames picked in the timeline (Shift+click widens it); a single frame when nothing else is picked.</summary>
    internal (int From, int To) FrameRange
    {
        get
        {
            int a = Math.Clamp(_rangeAnchor, 0, Session.Doc.Frames.Count - 1), b = Session.Frame;
            return a <= b ? (a, b) : (b, a);
        }
    }
    private int _rangeAnchor;

    /// <summary>Makes <paramref name="frame"/> current; with <paramref name="extend"/> the picked range grows to it.</summary>
    internal void GoToFrame(int frame, bool extend = false)
    {
        frame = Math.Clamp(frame, 0, Session.Doc.Frames.Count - 1);
        if (!extend) _rangeAnchor = frame;
        Session.Frame = frame;
        RefreshPanels();
    }

    private void NewFrame_Click(object? sender, RoutedEventArgs e) => NewFrame();
    private void NewEmptyFrame_Click(object? sender, RoutedEventArgs e) => NewEmptyFrame();
    private void RemoveFrame_Click(object? sender, RoutedEventArgs e) => RemoveFrame();
    private void FrameDuration_Click(object? sender, RoutedEventArgs e) => _ = FrameDurationAsync();
    private void FrameLeft_Click(object? sender, RoutedEventArgs e) => MoveFrame(-1);
    private void FrameRight_Click(object? sender, RoutedEventArgs e) => MoveFrame(+1);
    private void NewTag_Click(object? sender, RoutedEventArgs e) => _ = NewTagAsync();
    private void Play_Click(object? sender, RoutedEventArgs e) => TogglePlay();
    private void Loop_Click(object? sender, RoutedEventArgs e) => SetLoop(LoopItem.IsChecked);
    private void OnionSettings_Click(object? sender, RoutedEventArgs e) => _ = OnionSettingsAsync();
    private void Preview_Click(object? sender, RoutedEventArgs e) => ShowPreview(full: false);
    private void FullPreview_Click(object? sender, RoutedEventArgs e) => ShowPreview(full: true);

    /// <summary>Alt+N: a copy of the current frame right after it (Aseprite's "New Frame").</summary>
    internal void NewFrame()
    {
        Session.DropFloating();
        int f = Session.Frame;
        Session.Apply(FrameOps.Duplicate(Session.Doc, f));
        GoToFrame(f + 1);
    }

    internal void NewEmptyFrame()
    {
        Session.DropFloating();
        int f = Session.Frame;
        Session.Apply(AddFrameEdit.Perform(Session.Doc, new Frame { DurationMs = Session.Doc.Frames[f].DurationMs }, f + 1));
        GoToFrame(f + 1);
    }

    internal void RemoveFrame()
    {
        if (Session.Doc.Frames.Count == 1) { ShowStatus(L.T("A sprite needs at least one frame")); return; }
        Session.DropFloating();
        int f = Session.Frame;
        Session.Apply(RemoveFrameEdit.Perform(Session.Doc, f));
        GoToFrame(Math.Min(f, Session.Doc.Frames.Count - 1));
    }

    internal void MoveFrame(int delta)
    {
        int f = Session.Frame, to = f + delta;
        if (to < 0 || to >= Session.Doc.Frames.Count) return;
        Session.DropFloating();
        Session.Apply(MoveFrameEdit.Perform(Session.Doc, f, to));
        GoToFrame(to);
    }

    /// <summary>Sets the duration of the picked frames.</summary>
    internal async Task FrameDurationAsync()
    {
        var (from, to) = FrameRange;
        var current = Session.Doc.Frames[Session.Frame].DurationMs;
        var title = from == to ? L.F("Frame {0}", from + 1) : L.F("Frames {0} to {1}", from + 1, to + 1);
        if (await Dialogs.AskNumber(title, L.T("Duration (ms)"), current, 1, 65535) is not { } ms) return;
        Session.Apply(FrameDurationEdit.Perform(Session.Doc, Enumerable.Range(from, to - from + 1), ms));
    }

    // ---- tags ----

    private void ChangeTags(string editName, Action change)
    {
        var before = TagsEdit.Take(Session.Doc);
        change();
        Session.Apply(TagsEdit.Capture(Session.Doc, before, editName));
    }

    /// <summary>A tag over the picked frames.</summary>
    internal async Task NewTagAsync()
    {
        var (from, to) = FrameRange;
        var name = await Dialogs.AskText(L.T("New tag"), L.T("Name"), L.T("Tag") + " " + (Session.Doc.Tags.Count + 1));
        if (string.IsNullOrWhiteSpace(name)) return;
        var tag = new Tag { Name = name, From = from, To = to, Color = TagColors[Session.Doc.Tags.Count % TagColors.Length] };
        ChangeTags("New tag", () => Session.Doc.Tags.Add(tag));
    }

    internal async Task RenameTagAsync(Tag tag)
    {
        var name = await Dialogs.AskText(L.T("Rename tag"), L.T("Name"), tag.Name);
        if (string.IsNullOrWhiteSpace(name) || name == tag.Name) return;
        ChangeTags("Rename tag", () => tag.Name = name);
    }

    internal void SetTagDirection(Tag tag, LoopDir dir)
    {
        if (tag.Dir != dir) ChangeTags("Tag direction", () => tag.Dir = dir);
    }

    internal void SetTagColor(Tag tag, Rgba color)
    {
        if (tag.Color != color) ChangeTags("Tag colour", () => tag.Color = color);
    }

    /// <summary>Makes the tag cover the picked frames.</summary>
    internal void SetTagRange(Tag tag)
    {
        var (from, to) = FrameRange;
        if (tag.From != from || tag.To != to) ChangeTags("Tag frames", () => { tag.From = from; tag.To = to; });
    }

    internal void RemoveTag(Tag tag) => ChangeTags("Remove tag", () => Session.Doc.Tags.Remove(tag));

    // ---- playback ----

    public bool IsPlaying => _playTimer != null;

    /// <summary>Enter: plays from the current frame (the tag it is in, or everything), or stops.</summary>
    internal void TogglePlay()
    {
        if (IsPlaying) { StopPlaying(); return; }
        Session.DropFloating();
        Canvas.FinishGesture();
        _playback.Loop = LoopItem.IsChecked;
        _playback.Start(Session.Doc, Session.Frame);
        _playTimer = new DispatcherTimer { Interval = FrameTime(Session.Frame) };
        _playTimer.Tick += (_, _) => PlayStep();
        _playTimer.Start();
        RefreshPanels();
    }

    internal void StopPlaying()
    {
        _playTimer?.Stop();
        _playTimer = null;
        RefreshPanels();
    }

    /// <summary>One tick of playback: the next frame, or the end.</summary>
    internal void PlayStep()
    {
        if (_playback.Next(Session.Frame) is not { } next) { StopPlaying(); return; }
        _rangeAnchor = next;
        Session.Frame = next;
        if (_playTimer != null) _playTimer.Interval = FrameTime(next);
    }

    private TimeSpan FrameTime(int frame) => TimeSpan.FromMilliseconds(Math.Max(10, Session.Doc.Frames[frame].DurationMs));

    internal void SetLoop(bool loop)
    {
        LoopItem.IsChecked = loop;
        _playback.Loop = loop;
        Timeline.Rebuild();
    }

    internal Playback Playback => _playback;

    // ---- onion skin and preview ----

    internal void SetOnion(bool on)
    {
        OnionItem.IsChecked = on;
        Canvas.ShowOnion = on;
        Timeline.Rebuild();
    }

    private void ApplyOnionSettings()
    {
        var s = App.Settings;
        Canvas.Onion.Before = s.OnionBefore;
        Canvas.Onion.After = s.OnionAfter;
        Canvas.Onion.Opacity = (byte)Math.Clamp(s.OnionOpacity, 1, 255);
        Canvas.Onion.Tint = s.OnionTint;
        Canvas.RefreshAll();
    }

    internal async Task OnionSettingsAsync()
    {
        var s = App.Settings;
        if (await Dialogs.AskOnion(new OnionOptions(s.OnionBefore, s.OnionAfter, s.OnionOpacity, s.OnionTint)) is not { } o) return;
        (s.OnionBefore, s.OnionAfter, s.OnionOpacity, s.OnionTint) = (o.Before, o.After, o.Opacity, o.Tint);
        s.Save();
        ApplyOnionSettings();
    }

    private PreviewWindow? _preview;

    /// <summary>The floating preview (the animation zoomed 1x–8x) or, with <paramref name="full"/>, a full-screen view (F).</summary>
    internal PreviewWindow ShowPreview(bool full)
    {
        if (full)
        {
            var w = new PreviewWindow(this, full: true);
            w.Show(this);
            return w;
        }
        if (_preview == null)
        {
            _preview = new PreviewWindow(this, full: false);
            _preview.Closed += (_, _) => _preview = null;
            _preview.Show(this);
        }
        else _preview.Activate();
        return _preview;
    }
}
