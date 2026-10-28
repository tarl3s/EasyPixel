using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using EasyPixel.Core.Model;

namespace EasyPixel.Panels;

/// <summary>
/// The colour picker: a saturation/value square, a hue bar on its right and an alpha bar below. Raises
/// <see cref="ColorPicked"/> while the person drags; <see cref="Color"/> sets it from outside without raising it.
/// </summary>
public sealed class ColorPicker : Control
{
    private const double Gap = 6, Bar = 14;
    private const int SvRes = 64;

    private double _h, _s, _v = 0, _a = 1; // hue 0..360, the rest 0..1
    private WriteableBitmap? _sv, _hue, _alpha;
    private double _svHue = -1;
    private Rgba _alphaFor;
    private enum Part { None, Square, Hue, Alpha }
    private Part _dragging;

    public ColorPicker()
    {
        Height = 170;
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    public event Action<Rgba>? ColorPicked;

    public Rgba Color
    {
        get => FromHsv(_h, _s, _v, _a);
        set
        {
            if (value == Color) return;
            var (h, s, v) = ToHsv(value);
            if (s > 0 && v > 0) _h = h; // greys and black keep the hue the person had
            if (v > 0) _s = s;
            _v = v;
            _a = value.A / 255.0;
            InvalidateVisual();
        }
    }

    private Rect SquareRect => new(0, 0, Math.Max(10, Bounds.Width - Bar - Gap), Math.Max(10, Bounds.Height - Bar - Gap));
    private Rect HueRect => new(SquareRect.Right + Gap, 0, Bar, SquareRect.Height);
    private Rect AlphaRect => new(0, SquareRect.Bottom + Gap, SquareRect.Width, Bar);

    public override void Render(DrawingContext ctx)
    {
        var sq = SquareRect;
        using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.MediumQuality }))
        {
            ctx.DrawImage(SvBitmap(), new Rect(0, 0, SvRes, SvRes), sq);
            ctx.DrawImage(HueBitmap(), new Rect(0, 0, 1, 360), HueRect);
            ctx.DrawImage(AlphaBitmap(), new Rect(0, 0, 64, 1), AlphaRect);
        }
        var border = new Pen(Res.Brush("Border"), 1);
        ctx.DrawRectangle(null, border, sq);
        ctx.DrawRectangle(null, border, HueRect);
        ctx.DrawRectangle(null, border, AlphaRect);

        // Markers: a ring in the square, a line across each bar (black and white so they show on any colour).
        var p = new Point(sq.X + _s * sq.Width, sq.Y + (1 - _v) * sq.Height);
        ctx.DrawEllipse(null, new Pen(Brushes.Black, 3), p, 5, 5);
        ctx.DrawEllipse(null, new Pen(Brushes.White, 1.5), p, 5, 5);
        double hy = HueRect.Y + _h / 360 * HueRect.Height;
        Mark(ctx, new Rect(HueRect.X - 2, hy - 2, HueRect.Width + 4, 4));
        double ax = AlphaRect.X + _a * AlphaRect.Width;
        Mark(ctx, new Rect(ax - 2, AlphaRect.Y - 2, 4, AlphaRect.Height + 4));
    }

    private static void Mark(DrawingContext ctx, Rect r)
    {
        ctx.DrawRectangle(null, new Pen(Brushes.Black, 2), r);
        ctx.DrawRectangle(null, new Pen(Brushes.White, 1), r.Deflate(1));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        _dragging = SquareRect.Inflate(4).Contains(pos) ? Part.Square
            : HueRect.Inflate(4).Contains(pos) ? Part.Hue
            : AlphaRect.Inflate(4).Contains(pos) ? Part.Alpha : Part.None;
        if (_dragging == Part.None) return;
        e.Pointer.Capture(this);
        Pick(pos);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging != Part.None) Pick(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = Part.None;
        e.Pointer.Capture(null);
    }

    /// <summary>Picks at a point of the control (tests use it too).</summary>
    internal void Pick(Point pos)
    {
        static double Clamp01(double v) => Math.Clamp(v, 0, 1);
        switch (_dragging)
        {
            case Part.Square:
                _s = Clamp01((pos.X - SquareRect.X) / SquareRect.Width);
                _v = 1 - Clamp01((pos.Y - SquareRect.Y) / SquareRect.Height);
                break;
            case Part.Hue:
                _h = Clamp01((pos.Y - HueRect.Y) / HueRect.Height) * 359.999;
                break;
            case Part.Alpha:
                _a = Clamp01((pos.X - AlphaRect.X) / AlphaRect.Width);
                break;
        }
        InvalidateVisual();
        ColorPicked?.Invoke(Color);
    }

    /// <summary>Starts a pick on a part, as a press there would (tests).</summary>
    internal void PickSquare(double s, double v)
    {
        _dragging = Part.Square;
        Pick(new Point(SquareRect.X + s * SquareRect.Width, SquareRect.Y + (1 - v) * SquareRect.Height));
        _dragging = Part.None;
    }

    // ---- pictures ----

    private WriteableBitmap SvBitmap()
    {
        if (_sv != null && _svHue == _h) return _sv;
        _sv ??= New(SvRes, SvRes);
        Fill(_sv, (x, y) => FromHsv(_h, x / (SvRes - 1.0), 1 - y / (SvRes - 1.0), 1).Packed);
        _svHue = _h;
        return _sv;
    }

    private WriteableBitmap HueBitmap()
    {
        if (_hue != null) return _hue;
        _hue = New(1, 360);
        Fill(_hue, (_, y) => FromHsv(y, 1, 1, 1).Packed);
        return _hue;
    }

    private WriteableBitmap AlphaBitmap()
    {
        var c = Color with { A = 255 };
        if (_alpha != null && _alphaFor == c) return _alpha;
        _alpha ??= New(64, 1);
        // Mixed over a light grey, so "transparent" reads as the grey on the left.
        Fill(_alpha, (x, _) =>
        {
            double a = x / 63.0;
            byte Mix(byte ch) => (byte)Math.Round(ch * a + 200 * (1 - a));
            return new Rgba(Mix(c.R), Mix(c.G), Mix(c.B), 255).Packed;
        });
        _alphaFor = c;
        return _alpha;
    }

    private static WriteableBitmap New(int w, int h) =>
        new(new PixelSize(w, h), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);

    private static unsafe void Fill(WriteableBitmap bmp, Func<int, int, uint> color)
    {
        using var fb = bmp.Lock();
        for (int y = 0; y < fb.Size.Height; y++)
        {
            var row = new Span<uint>((byte*)fb.Address + (long)y * fb.RowBytes, fb.Size.Width);
            for (int x = 0; x < row.Length; x++) row[x] = color(x, y);
        }
    }

    // ---- HSV ----

    public static Rgba FromHsv(double h, double s, double v, double a)
    {
        double c = v * s, hh = (h % 360) / 60, x = c * (1 - Math.Abs(hh % 2 - 1)), m = v - c;
        var (r, g, b) = hh switch
        {
            < 1 => (c, x, 0d), < 2 => (x, c, 0d), < 3 => (0d, c, x), < 4 => (0d, x, c), < 5 => (x, 0d, c), _ => (c, 0d, x),
        };
        static byte B(double d) => (byte)Math.Clamp(Math.Round(d * 255), 0, 255);
        return new Rgba(B(r + m), B(g + m), B(b + m), B(a));
    }

    public static (double H, double S, double V) ToHsv(Rgba c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double h = d == 0 ? 0 : max == r ? 60 * (((g - b) / d) % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        if (h < 0) h += 360;
        return (h, max == 0 ? 0 : d / max, max);
    }
}
