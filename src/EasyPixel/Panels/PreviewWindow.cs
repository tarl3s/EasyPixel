using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EasyPixel.Core.Render;

namespace EasyPixel.Panels;

/// <summary>
/// The preview: the current frame (it follows playback) zoomed 1x–8x in a small window that floats over the editor,
/// or, as the full preview (F), filling the screen; Escape or a click closes the full preview.
/// </summary>
public sealed class PreviewWindow : Window
{
    private readonly MainWindow _main;
    private readonly bool _full;
    private readonly Avalonia.Controls.Image _image = new() { Stretch = Stretch.Uniform };
    private readonly ComboBox _zoom = new() { ItemsSource = Enumerable.Range(1, 8).Select(z => z + "x").ToArray(), SelectedIndex = 1, Width = 80 };
    private readonly DispatcherTimer _redraw;
    private Core.Model.Image? _flat;
    private WriteableBitmap? _bitmap;
    private bool _dirty = true;

    public PreviewWindow(MainWindow main, bool full)
    {
        _main = main;
        _full = full;
        Title = L.T("Preview");
        ShowInTaskbar = false;
        RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);
        if (full)
        {
            WindowState = WindowState.FullScreen;
            SystemDecorations = SystemDecorations.None;
            Background = Res.Brush("Viewport");
            Content = new Border { Padding = new Thickness(24), Child = _image };
            KeyDown += (_, _) => Close();
            PointerPressed += (_, _) => Close();
        }
        else
        {
            Width = 260;
            Height = 300;
            Topmost = true;
            var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(6) };
            bar.Children.Add(new TextBlock { Text = L.T("Zoom"), VerticalAlignment = VerticalAlignment.Center });
            bar.Children.Add(_zoom);
            var dock = new DockPanel();
            DockPanel.SetDock(bar, Dock.Top);
            dock.Children.Add(bar);
            dock.Children.Add(new ScrollViewer
            {
                Content = _image, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Res.Brush("Viewport"),
            });
            Content = dock;
            _zoom.SelectionChanged += (_, _) => Layout();
        }
        // Redraw at most ~30 times a second, and only after something changed (a 4096² flatten per stroke step would be too slow).
        _redraw = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background, (_, _) => { if (_dirty) Redraw(); });
        main.Canvas.PictureChanged += MarkDirty;
        Opened += (_, _) => { Redraw(); _redraw.Start(); };
        Closed += (_, _) => { _redraw.Stop(); main.Canvas.PictureChanged -= MarkDirty; };
    }

    /// <summary>1–8.</summary>
    public int Zoom
    {
        get => _zoom.SelectedIndex + 1;
        set => _zoom.SelectedIndex = Math.Clamp(value, 1, 8) - 1;
    }

    /// <summary>The frame shown last (tests).</summary>
    internal int ShownFrame { get; private set; } = -1;

    private void MarkDirty() => _dirty = true;

    internal void Redraw()
    {
        _dirty = false;
        var s = _main.Session;
        var doc = s.Doc;
        if (_flat == null || _flat.Width != doc.Width || _flat.Height != doc.Height)
        {
            _flat = new Core.Model.Image(doc.Width, doc.Height);
            _bitmap = new WriteableBitmap(new PixelSize(doc.Width, doc.Height), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        }
        Compositor.Flatten(doc, s.Frame, _flat, s.CanvasRect);
        using (var fb = _bitmap!.Lock())
        {
            unsafe
            {
                for (int y = 0; y < doc.Height; y++)
                    _flat.Pixels.AsSpan(y * doc.Width, doc.Width).CopyTo(new Span<uint>((byte*)fb.Address + (long)y * fb.RowBytes, doc.Width));
            }
        }
        ShownFrame = s.Frame;
        _image.Source = null; // the same bitmap object: make the image take the new pixels
        _image.Source = _bitmap;
        Layout();
    }

    private void Layout()
    {
        if (_full || _bitmap == null) return;
        _image.Width = _bitmap.PixelSize.Width * Zoom;
        _image.Height = _bitmap.PixelSize.Height * Zoom;
    }
}
