using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EasyPixel.Core.Learn;

namespace EasyPixel.Learn;

/// <summary>
/// A lesson illustration on its backdrop, drawn at an integer zoom without smoothing. Animated ones play while the
/// view is on screen.
/// </summary>
public sealed class ArtView : Border
{
    private readonly ArtExample _art;
    private readonly WriteableBitmap[] _frames;
    private readonly Avalonia.Controls.Image _image = new() { Stretch = Stretch.Fill };
    private readonly DispatcherTimer? _timer;
    private int _frame;

    public ArtView(ArtExample art, int zoom)
    {
        _art = art;
        Background = Res.Brush("ArtBackdrop");
        BorderBrush = Res.Brush("Border");
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8);
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);
        var s = art.Sprite;
        _frames = new WriteableBitmap[s.Frames.Count];
        for (int f = 0; f < _frames.Length; f++) _frames[f] = ToBitmap(s.Cels[(s.Layers[0], f)].Image);
        _image.Source = _frames[0];
        Zoom = zoom;
        Child = _image;
        if (_frames.Length > 1)
        {
            _timer = new DispatcherTimer { Interval = Duration(0) };
            _timer.Tick += (_, _) => Step();
            AttachedToVisualTree += (_, _) => _timer.Start();
            DetachedFromVisualTree += (_, _) => _timer.Stop();
        }
    }

    public string Id => _art.Id;
    public ArtExample Art => _art;
    public int Frame => _frame;
    public bool Animated => _frames.Length > 1;

    /// <summary>The zoom the picture is shown at (1 = one screen pixel per pixel).</summary>
    public int Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Max(1, value);
            _image.Width = _art.Sprite.Width * _zoom;
            _image.Height = _art.Sprite.Height * _zoom;
        }
    }
    private int _zoom = 1;

    /// <summary>Shows the next frame (the timer calls it; tests too).</summary>
    internal void Step()
    {
        _frame = (_frame + 1) % _frames.Length;
        _image.Source = _frames[_frame];
        if (_timer != null) _timer.Interval = Duration(_frame);
    }

    private TimeSpan Duration(int f) => TimeSpan.FromMilliseconds(Math.Max(30, _art.Sprite.Frames[f].DurationMs));

    internal static WriteableBitmap ToBitmap(Core.Model.Image img)
    {
        var bmp = new WriteableBitmap(new PixelSize(img.Width, img.Height), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        using var fb = bmp.Lock();
        unsafe
        {
            for (int y = 0; y < img.Height; y++)
                img.Pixels.AsSpan(y * img.Width, img.Width).CopyTo(new Span<uint>((byte*)fb.Address + (long)y * fb.RowBytes, img.Width));
        }
        return bmp;
    }
}
