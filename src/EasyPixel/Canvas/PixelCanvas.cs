using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;
using EasyPixel.Tools;

namespace EasyPixel.Canvas;

/// <summary>
/// The drawing surface: shows the current frame (all visible layers) over a checkerboard, with optional onion skin,
/// pixel grid, selection outline ("marching ants") and tool guides; zooms 1%–6400% around the pointer (wheel), pans with
/// Space+drag or the middle button, and sends presses in canvas pixels to the current tool. Only the changed area of
/// the picture is recomposited and copied into the bitmap.
/// </summary>
public sealed partial class PixelCanvas : Control
{
    /// <summary>Zoom steps in percent, as the wheel and Zoom in/out walk them.</summary>
    public static readonly double[] ZoomLevels =
    {
        1, 2, 3, 5, 8, 10, 12.5, 100 / 6.0, 25, 100 / 3.0, 50, 200 / 3.0, 100, 200, 300, 400, 500, 600, 800, 1000, 1200,
        1600, 2000, 2400, 3200, 4000, 4800, 5600, 6400,
    };

    public const double MinScale = 0.01, MaxScale = 64;

    private readonly CompositeScratch _scratch = new();
    private EditorSession? _s;
    private ToolController? _tools;
    private ToolContext? _ctx;
    private Image? _composite; // the frame as it looks (without onion skin), sprite-sized RGBA
    private Image? _onion;
    private WriteableBitmap? _bitmap; // what is drawn: onion skin under the composite
    private WriteableBitmap? _floatBitmap;
    private FloatingState? _floatFor;
    private int _floatVersion;
    private ImageBrush? _checker;
    private bool _needsFit = true;

    private bool _panning;
    private Point _panStart;
    private Vector _panOffsetStart;
    private Tool? _activeTool;
    private ToolInput _lastInput;

    public PixelCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        _antsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _antsTimer.Tick += (_, _) => { _antsPhase = (_antsPhase + 1) % 8; InvalidateVisual(); };
    }

    /// <summary>Size of one sprite pixel on screen (1 = 100%).</summary>
    public double Scale { get; private set; } = 1;
    /// <summary>Where the sprite's top-left corner is, in control coordinates.</summary>
    public Vector Offset { get; private set; }

    public bool ShowGrid { get => _showGrid; set { _showGrid = value; InvalidateVisual(); } }
    private bool _showGrid = true;

    public bool ShowOnion { get => _showOnion; set { _showOnion = value; RefreshAll(); } }
    private bool _showOnion;
    public OnionSettings Onion { get; } = new();

    /// <summary>Space is held: a left drag pans instead of drawing.</summary>
    public bool SpaceHeld { get; set; }

    /// <summary>The pixel under the pointer, or null when the pointer is off the canvas.</summary>
    public (int X, int Y)? Hover { get; private set; }

    /// <summary>Zoom, scroll or hover changed (for the status bar).</summary>
    public event Action? ViewChanged;

    public EditorSession? Session => _s;

    /// <summary>Shows <paramref name="s"/>, drawing with <paramref name="tools"/>; <paramref name="status"/> receives tool messages.</summary>
    public void Attach(EditorSession s, ToolController tools, Action<string> status)
    {
        if (_s != null)
        {
            _s.Changed -= RefreshArea;
            _s.SelectionChanged -= OnSelectionChanged;
        }
        _s = s;
        _tools = tools;
        _ctx = new ToolContext(s, tools, PickComposite, status);
        _activeTool = null;
        s.Changed += RefreshArea;
        s.SelectionChanged += OnSelectionChanged;
        _composite = null;
        RefreshAll();
        OnSelectionChanged();
        _needsFit = true;
        if (Bounds.Width > 0) FitToView();
    }

    internal ToolContext? Context => _ctx;

    // ---- picture ----

    public void RefreshAll()
    {
        if (_s != null) RefreshArea(_s.CanvasRect);
    }

    /// <summary>Recomposites <paramref name="area"/> (canvas pixels) and copies it into the bitmap.</summary>
    public void RefreshArea(PixelRect area)
    {
        if (_s == null) return;
        var doc = _s.Doc;
        if (_composite == null || _composite.Width != doc.Width || _composite.Height != doc.Height)
        {
            _composite = new Image(doc.Width, doc.Height);
            _onion = null;
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(doc.Width, doc.Height), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
            area = _s.CanvasRect;
        }
        area = area.Intersect(_s.CanvasRect);
        if (area.IsEmpty) return;
        Compositor.Flatten(doc, _s.Frame, _composite, area, scratch: _scratch);
        bool onion = _showOnion && doc.Frames.Count > 1;
        if (onion)
        {
            if (_onion == null || _onion.Width != doc.Width || _onion.Height != doc.Height) _onion = new Image(doc.Width, doc.Height);
            Core.Render.Onion.Render(doc, _s.Frame, Onion, _onion, area, _scratch);
        }
        CopyToBitmap(area, onion);
        InvalidateVisual();
    }

    private unsafe void CopyToBitmap(PixelRect r, bool onion)
    {
        var src = _composite!.Pixels;
        int w = _composite.Width;
        using var fb = _bitmap!.Lock();
        for (int y = r.Y; y < r.Bottom; y++)
        {
            var row = new Span<uint>((byte*)fb.Address + (long)y * fb.RowBytes + r.X * 4L, r.W);
            var from = src.AsSpan(y * w + r.X, r.W);
            if (!onion) { from.CopyTo(row); continue; }
            var ghost = _onion!.Pixels.AsSpan(y * w + r.X, r.W);
            for (int i = 0; i < row.Length; i++)
            {
                uint p = from[i];
                uint a = p >> 24;
                row[i] = a == 255 ? p : a == 0 ? ghost[i] : BlendModes.Blend(BlendMode.Normal, ghost[i], p, 255);
            }
        }
    }

    private Rgba PickComposite(int x, int y) =>
        _composite != null && (uint)x < (uint)_composite.Width && (uint)y < (uint)_composite.Height
            ? Rgba.FromPacked(_composite.Pixels[y * _composite.Width + x])
            : Rgba.Transparent;

    /// <summary>The pixel as copied into the canvas bitmap (onion skin included), for the tests.</summary>
    internal unsafe uint ShownPixel(int x, int y)
    {
        using var fb = _bitmap!.Lock();
        return *(uint*)((byte*)fb.Address + (long)y * fb.RowBytes + x * 4L);
    }

    /// <summary>A picture of the floating pixels (palette colours resolved), rebuilt only when they change.</summary>
    private WriteableBitmap? FloatBitmap()
    {
        var fs = _s?.Float;
        if (fs == null) { _floatFor = null; return null; }
        if (ReferenceEquals(fs, _floatFor) && fs.Version == _floatVersion && _floatBitmap != null) return _floatBitmap;
        var img = fs.Pixels.Pixels;
        _floatBitmap?.Dispose();
        _floatBitmap = new WriteableBitmap(new PixelSize(img.Width, img.Height), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        var doc = _s!.Doc;
        bool indexed = doc.Mode == ColorMode.Indexed;
        using (var fb = _floatBitmap.Lock())
        {
            unsafe
            {
                for (int y = 0; y < img.Height; y++)
                {
                    var row = new Span<uint>((byte*)fb.Address + (long)y * fb.RowBytes, img.Width);
                    for (int x = 0; x < img.Width; x++)
                    {
                        uint v = img.Pixels[y * img.Width + x];
                        row[x] = v == 0 ? 0 : !indexed ? v
                            : (int)v == doc.Palette.TransparentIndex || v >= (uint)doc.Palette.Colors.Count ? 0 : doc.Palette.Colors[(int)v].Packed;
                    }
                }
            }
        }
        _floatFor = fs;
        _floatVersion = fs.Version;
        return _floatBitmap;
    }

    // ---- view ----

    public double ZoomPercent => Scale * 100;

    /// <summary>Zooms to <paramref name="scale"/> keeping the sprite point under <paramref name="anchor"/> (control coordinates) still.</summary>
    public void ZoomAt(double scale, Point anchor)
    {
        scale = Math.Clamp(scale, MinScale, MaxScale);
        var sprite = (anchor - Offset) / Scale;
        Scale = scale;
        Offset = anchor - sprite * scale;
        _needsFit = false;
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    public void ZoomStep(int steps, Point? anchor = null)
    {
        double pct = Scale * 100;
        int i = steps > 0 ? Array.FindIndex(ZoomLevels, z => z > pct + 1e-6) : Array.FindLastIndex(ZoomLevels, z => z < pct - 1e-6);
        if (i < 0) return;
        i = Math.Clamp(i + (steps > 0 ? steps - 1 : steps + 1), 0, ZoomLevels.Length - 1);
        ZoomAt(ZoomLevels[i] / 100, anchor ?? new Point(Bounds.Width / 2, Bounds.Height / 2));
    }

    /// <summary>The largest zoom step at which the whole sprite fits, centred.</summary>
    public void FitToView()
    {
        if (_s == null || Bounds.Width <= 0 || Bounds.Height <= 0) { _needsFit = true; return; }
        double fit = Math.Min(Bounds.Width * 0.9 / _s.Doc.Width, Bounds.Height * 0.9 / _s.Doc.Height) * 100;
        double pct = ZoomLevels.LastOrDefault(z => z <= fit, ZoomLevels[0]);
        Scale = pct / 100;
        Offset = new Vector((Bounds.Width - _s.Doc.Width * Scale) / 2, (Bounds.Height - _s.Doc.Height * Scale) / 2);
        _needsFit = false;
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    /// <summary>Control point of the top-left corner of canvas pixel (x, y).</summary>
    public Point SpriteToScreen(double x, double y) => new(Offset.X + x * Scale, Offset.Y + y * Scale);

    public (int X, int Y) ScreenToSprite(Point p) =>
        ((int)Math.Floor((p.X - Offset.X) / Scale), (int)Math.Floor((p.Y - Offset.Y) / Scale));

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_needsFit) FitToView();
    }

    // ---- input ----

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var pt = e.GetCurrentPoint(this);
        var props = pt.Properties;
        if (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && SpaceHeld))
        {
            _panning = true;
            _panStart = pt.Position;
            _panOffsetStart = Offset;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }
        if (_ctx == null || _tools == null || _activeTool != null || !(props.IsLeftButtonPressed || props.IsRightButtonPressed)) return;
        var tool = _tools.CurrentTool;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && ToolController.IsPaintTool(tool.Kind)) tool = _tools.Get(ToolKind.Eyedropper);
        _lastInput = Input(pt.Position, e.KeyModifiers, props.IsRightButtonPressed && !props.IsLeftButtonPressed);
        _activeTool = tool;
        e.Pointer.Capture(this);
        tool.Down(_ctx, _lastInput);
        if (!tool.Active) _activeTool = null;
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        if (_panning)
        {
            Offset = _panOffsetStart + (pos - _panStart);
            InvalidateVisual();
            ViewChanged?.Invoke();
            return;
        }
        var (x, y) = ScreenToSprite(pos);
        var hover = _s != null && _s.CanvasRect.Contains(x, y) ? (x, y) : ((int, int)?)null;
        if (hover != Hover) { Hover = hover; ViewChanged?.Invoke(); }
        if (_activeTool != null && _ctx != null)
        {
            _lastInput = Input(pos, e.KeyModifiers, _lastInput.Right);
            _activeTool.Move(_ctx, _lastInput);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            return;
        }
        if (_activeTool != null)
        {
            _lastInput = Input(e.GetPosition(this), e.KeyModifiers, _lastInput.Right);
            FinishGesture();
        }
        e.Pointer.Capture(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _panning = false;
        FinishGesture(); // keep what was drawn rather than losing it
    }

    private void FinishGesture()
    {
        var tool = _activeTool;
        _activeTool = null;
        if (tool != null && _ctx != null) tool.Up(_ctx, _lastInput);
        InvalidateVisual();
    }

    /// <summary>Escape: abandons the gesture in progress. Returns false when there was none.</summary>
    public bool CancelGesture()
    {
        if (_activeTool == null || _ctx == null) return false;
        _activeTool.Cancel(_ctx);
        _activeTool = null;
        InvalidateVisual();
        return true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y == 0) return;
        ZoomStep(e.Delta.Y > 0 ? 1 : -1, e.GetPosition(this));
        e.Handled = true;
    }

    private ToolInput Input(Point pos, KeyModifiers m, bool right)
    {
        var (x, y) = ScreenToSprite(pos);
        return new ToolInput(x, y, right, m.HasFlag(KeyModifiers.Shift), m.HasFlag(KeyModifiers.Alt), m.HasFlag(KeyModifiers.Control));
    }
}
