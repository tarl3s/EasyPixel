using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Learn;

namespace EasyPixel.Ai;

/// <summary>
/// File → ✨ Create with AI: a description in any language → 4 pixel-art candidates → the chosen one as a new sprite or
/// a new layer (<see cref="Result"/>; null = cancelled).
/// </summary>
internal sealed class CreateWithAiWindow : Window
{
    public const int MaxRequest = 1000;

    private readonly AiCreateRequest _start;
    private readonly Func<AiCreateSession> _newSession;
    private readonly int[] _sizes;
    private readonly ComboBox _size, _view, _palette;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 560 };
    private readonly TextBlock _note = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 560, Classes = { "dim" }, IsVisible = false };
    // what the AI was actually asked (the request in English), so a lost detail in the translation can be seen
    private readonly TextBlock _sent = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 560, Classes = { "dim" }, FontSize = 11, IsVisible = false };
    private readonly Button _generate, _more, _useSprite, _addLayer;
    private readonly ToggleButton[] _slotButtons = new ToggleButton[AiCreateSession.Slots];
    private readonly Avalonia.Controls.Image[] _big = new Avalonia.Controls.Image[AiCreateSession.Slots], _small = new Avalonia.Controls.Image[AiCreateSession.Slots];
    private readonly TextBlock[] _slotText = new TextBlock[AiCreateSession.Slots];
    private CancellationTokenSource? _cts;
    private IReadOnlyList<Rgba>? _filePalette;
    private int _paletteShown;
    private int _selected = -1;
    private bool _busy;

    public CreateWithAiWindow(AiCreateRequest start, Func<AiCreateSession> newSession)
    {
        _start = start;
        _newSession = newSession;
        Title = L.T("✨ Create with AI");
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        RequestBox = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 64, Width = 560,
            Watermark = L.T("Describe the sprite, in any language: \"a knight with a blue cape\"") };
        var sizes = new List<int> { 16, 32, 48, 64 };
        var sizeNames = sizes.Select(s => $"{s} x {s}").ToList();
        if (start.CanvasSize > 0 && !sizes.Contains(start.CanvasSize))
        {
            sizes.Add(start.CanvasSize);
            sizeNames.Add(L.F("This canvas ({0} x {0})", start.CanvasSize));
        }
        _sizes = sizes.ToArray();
        int def = start.CanvasSize > 0 && sizes.Contains(start.CanvasSize) ? sizes.IndexOf(start.CanvasSize) : 1;
        _size = new ComboBox { ItemsSource = sizeNames, SelectedIndex = def, MinWidth = 160 };
        _view = new ComboBox { ItemsSource = new[] { L.T("Automatic"), L.T("Side"), L.T("Front"), L.T("Top-down") }, SelectedIndex = 0, MinWidth = 160 };
        _palette = new ComboBox { ItemsSource = new[] { L.T("Automatic"), L.T("This sprite's palette"), L.T("From a file…") }, SelectedIndex = 0, MinWidth = 160 };
        _palette.SelectionChanged += (_, _) => _ = PaletteChangedAsync();

        _generate = new Button { Content = L.T("✨ Generate"), IsDefault = true, IsEnabled = false, Classes = { "accent" } };
        RequestBox.TextChanged += (_, _) => { _generate.IsEnabled = !_busy && HasRequest; };
        _generate.Click += (_, _) => Running = GenerateAsync();
        _more = new Button { Content = L.T("4 more"), IsEnabled = false };
        _more.Click += (_, _) => Running = GenerateAsync();
        _useSprite = new Button { Content = L.T("Use as new sprite"), IsEnabled = false };
        _useSprite.Click += (_, _) => Finish(false);
        _addLayer = new Button { Content = L.T("Add as a new layer"), IsEnabled = false };
        _addLayer.Click += (_, _) => Finish(true);
        var cancel = new Button { Content = L.T("Cancel"), IsCancel = true };
        cancel.Click += (_, _) => Close();

        var options = new WrapPanel { Orientation = Orientation.Horizontal };
        options.Children.Add(Field(L.T("Size"), _size));
        options.Children.Add(Field(L.T("View"), _view));
        options.Children.Add(Field(L.T("Palette"), _palette));

        var slots = new UniformGrid { Columns = AiCreateSession.Slots };
        for (int i = 0; i < AiCreateSession.Slots; i++)
        {
            int slot = i;
            _big[i] = new Avalonia.Controls.Image { Width = 128, Height = 128, Stretch = Stretch.Uniform };
            _small[i] = new Avalonia.Controls.Image { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Center };
            RenderOptions.SetBitmapInterpolationMode(_big[i], Avalonia.Media.Imaging.BitmapInterpolationMode.None);
            _slotText[i] = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 128, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
            var stack = new StackPanel { Spacing = 4, Width = 132 };
            stack.Children.Add(new Panel { Height = 128, Children = { _big[i], _slotText[i] } });
            stack.Children.Add(_small[i]);
            _slotButtons[i] = new ToggleButton { Content = stack, Margin = new Thickness(4), IsEnabled = false, Padding = new Thickness(4) };
            _slotButtons[i].Click += (_, _) => Select(slot);
            slots.Children.Add(_slotButtons[i]);
        }

        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        top.Children.Add(_generate);
        top.Children.Add(_more);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        buttons.Children.Add(_useSprite);
        if (start.CanvasSize > 0) buttons.Children.Add(_addLayer);
        buttons.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        root.Children.Add(RequestBox);
        root.Children.Add(options);
        root.Children.Add(top);
        root.Children.Add(_note);
        root.Children.Add(slots);
        root.Children.Add(_status);
        root.Children.Add(_sent);
        root.Children.Add(buttons);
        Content = root;
        Closed += (_, _) => _cts?.Cancel();
    }

    protected override Type StyleKeyOverride => typeof(Window);

    private static Control Field(string label, Control c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 0, 16, 4) };
        p.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        p.Children.Add(c);
        return p;
    }

    internal TextBox RequestBox { get; }

    private bool HasRequest => !string.IsNullOrWhiteSpace(RequestBox.Text);
    internal bool CanGenerate => _generate.IsEnabled;

    private static void DebugTrace(Exception ex) => System.Diagnostics.Debug.WriteLine(ex);

    /// <summary>The chosen candidate, or null when the window was cancelled.</summary>
    public AiPick? Result { get; private set; }

    /// <summary>The last Generate / 4 more (tests await it).</summary>
    internal Task Running { get; private set; } = Task.CompletedTask;

    internal Image?[] Candidates { get; } = new Image?[AiCreateSession.Slots];
    internal string?[] Errors { get; } = new string?[AiCreateSession.Slots];
    internal string Status => _status.Text ?? "";
    internal string? Note => _note.IsVisible ? _note.Text : null;
    internal bool CanAddLayer => _start.CanvasSize > 0 && _addLayer.IsEnabled;
    internal int SizeIndex { get => _size.SelectedIndex; set => _size.SelectedIndex = value; }
    internal int PaletteIndex { get => _palette.SelectedIndex; set => _palette.SelectedIndex = value; }

    /// <summary>Tests: the palette that "From a file…" would have read.</summary>
    internal void UsePaletteFile(IReadOnlyList<Rgba> colors) { _filePalette = colors; _paletteShown = 2; _palette.SelectedIndex = 2; }

    /// <summary>The palette the next Generate uses (null = automatic), with the note shown when the choice can't be used.</summary>
    internal IReadOnlyList<Rgba>? ChosenPalette()
    {
        _note.IsVisible = false;
        switch (_palette.SelectedIndex)
        {
            case 1 when _start.SpritePalette is { Count: >= 3 } p: return p;
            case 1:
                _note.Text = L.T("This sprite has fewer than 3 colours, so the colours are chosen automatically.");
                _note.IsVisible = true;
                return null;
            case 2: return _filePalette;
            default: return null;
        }
    }

    private async Task PaletteChangedAsync()
    {
        if (_palette.SelectedIndex != 2 || _paletteShown == 2) { _paletteShown = _palette.SelectedIndex; ChosenPalette(); return; }
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("Palette"),
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(L.T("All supported files")) { Patterns = PaletteFormats.ReadableExtensions.Select(e => "*" + e).ToArray() }],
        });
        List<Rgba>? colors = null;
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            try { colors = PaletteFormats.Read(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or InvalidDataException) { }
        }
        if (colors is { Count: > 0 }) { _filePalette = colors; _paletteShown = 2; }
        else
        {
            if (files.Count > 0) _status.Text = L.T("That palette file couldn't be read.");
            _paletteShown = 0;
            _palette.SelectedIndex = 0;
        }
    }

    internal async Task GenerateAsync()
    {
        var request = (RequestBox.Text ?? "").Trim();
        if (request.Length == 0) { _status.Text = L.T("Describe the sprite first."); return; }
        if (request.Length > MaxRequest) request = request[..MaxRequest].TrimEnd(); // a huge paste is cut, not refused

        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        var palette = ChosenPalette();
        int size = _sizes[Math.Max(0, _size.SelectedIndex)];
        var view = (SpriteView)Math.Max(0, _view.SelectedIndex);
        Select(-1);
        for (int i = 0; i < AiCreateSession.Slots; i++)
        {
            Candidates[i] = null; Errors[i] = null;
            _big[i].Source = null; _small[i].Source = null;
            _slotText[i].Text = L.T("Drawing…");
            _slotButtons[i].IsEnabled = false;
        }
        _generate.IsEnabled = _more.IsEnabled = false;
        _busy = true;
        _status.Text = L.T("The AI is drawing 4 sprites. This can take a minute.");

        var session = _newSession();
        session.CandidateReady += (slot, sprite, error) =>
        {
            if (cts.IsCancellationRequested) return;
            Candidates[slot] = sprite;
            Errors[slot] = error;
            if (sprite != null)
            {
                _big[slot].Source = ArtView.ToBitmap(sprite);
                _small[slot].Source = ArtView.ToBitmap(sprite);
                _slotText[slot].Text = "";
                _slotButtons[slot].IsEnabled = true;
                if (_selected < 0) Select(slot);
            }
            else _slotText[slot].Text = "✗ " + error;
        };
        try
        {
            await session.RunAsync(request, size, view, palette, cts.Token);
            if (session.English is { Length: > 0 } en)
            {
                _sent.Text = L.F("Asked the AI (in English): {0}", SpritePrompt.Soften(en));
                L.Keep(_sent); // holds the person's words
                _sent.IsVisible = true;
            }
            _status.Text = Candidates.Any(c => c != null) ? L.T("Pick the one you like, or ask for 4 more.") : L.T("No sprite could be made. Try again or change the description.");
        }
        catch (OperationCanceledException) { return; } // cancelled: stop quietly
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _status.Text = L.T("Something went wrong while drawing. Try again.");
            DebugTrace(ex);
        }
        finally
        {
            if (_cts == cts) { _busy = false; _generate.IsEnabled = HasRequest; _more.IsEnabled = true; }
        }
    }

    internal void Select(int slot)
    {
        _selected = slot >= 0 && Candidates[slot] != null ? slot : -1;
        for (int i = 0; i < AiCreateSession.Slots; i++) _slotButtons[i].IsChecked = i == _selected;
        _useSprite.IsEnabled = _addLayer.IsEnabled = _selected >= 0;
    }

    internal void Finish(bool asLayer)
    {
        if (_selected < 0 || Candidates[_selected] is not { } sprite) return;
        if (asLayer && _start.CanvasSize <= 0) return;
        Result = new AiPick(sprite, asLayer);
        Close();
    }
}
