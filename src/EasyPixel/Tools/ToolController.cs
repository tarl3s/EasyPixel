using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tools;

public enum ToolKind
{
    Pencil, Eraser, Bucket, Line, Rectangle, Ellipse,
    RectSelect, EllipseSelect, Lasso, MagicWand, Move, Eyedropper,
    Shading, LightenDarken, Dither, Spray,
}

/// <summary>The current tool and every drawing setting the tools share: colours, brush, mirrors, dither, fill options.</summary>
public sealed class ToolController
{
    private readonly Dictionary<ToolKind, Tool> _tools;
    private ToolKind _current = ToolKind.Pencil;

    public ToolController()
    {
        _tools = new Tool[]
        {
            new FreehandTool(ToolKind.Pencil, "Pencil"), new FreehandTool(ToolKind.Eraser, "Eraser"),
            new FreehandTool(ToolKind.Shading, "Shading"), new FreehandTool(ToolKind.LightenDarken, "Lighten / darken"),
            new FreehandTool(ToolKind.Dither, "Dither brush"), new FreehandTool(ToolKind.Spray, "Spray"),
            new BucketTool(), new ShapeTool(ToolKind.Line, "Line"), new ShapeTool(ToolKind.Rectangle, "Rectangle"),
            new ShapeTool(ToolKind.Ellipse, "Ellipse"), new SelectTool(ToolKind.RectSelect), new SelectTool(ToolKind.EllipseSelect),
            new SelectTool(ToolKind.Lasso), new MagicWandTool(), new MoveTool(), new EyedropperTool(),
        }.ToDictionary(t => t.Kind);
    }

    /// <summary>Raised when the tool or any setting changes.</summary>
    public event Action? Changed;

    public ToolKind Current
    {
        get => _current;
        set { if (_current != value) { _current = value; Changed?.Invoke(); } }
    }

    public Tool CurrentTool => _tools[_current];
    public Tool Get(ToolKind kind) => _tools[kind];

    public Rgba Primary { get; private set; } = new(0, 0, 0, 255);
    public Rgba Secondary { get; private set; } = new(255, 255, 255, 255);

    public void SetPrimary(Rgba c) { Primary = c; Changed?.Invoke(); }
    public void SetSecondary(Rgba c) { Secondary = c; Changed?.Invoke(); }
    public void SwapColors() { (Primary, Secondary) = (Secondary, Primary); Changed?.Invoke(); }

    public Brush Brush { get; } = new();
    public void SetBrush(int size, BrushShape shape) { Brush.Size = Math.Clamp(size, 1, 64); Brush.Shape = shape; Changed?.Invoke(); }

    public bool MirrorH { get; private set; }
    public bool MirrorV { get; private set; }
    public void SetMirror(bool h, bool v) { MirrorH = h; MirrorV = v; Changed?.Invoke(); }

    public DitherKind Dither { get; private set; } = DitherKind.Checker;
    public void SetDither(DitherKind k) { Dither = k; Changed?.Invoke(); }

    /// <summary>Freehand pencil/eraser strokes drop the corner pixel of every "L" step (one-pixel brush only).</summary>
    public bool PixelPerfect { get; private set; }
    public void SetPixelPerfect(bool on) { PixelPerfect = on; Changed?.Invoke(); }

    /// <summary>Rectangle and ellipse are filled instead of outlined.</summary>
    public bool Filled { get; private set; }
    public void SetFilled(bool on) { Filled = on; Changed?.Invoke(); }

    /// <summary>Bucket and magic wand: only the touching region (true) or every matching pixel.</summary>
    public bool Contiguous { get; private set; } = true;
    /// <summary>Bucket and magic wand: how far (0..255 per channel) a colour may be from the clicked one.</summary>
    public int Tolerance { get; private set; }
    public void SetFillOptions(bool contiguous, int tolerance) { Contiguous = contiguous; Tolerance = Math.Clamp(tolerance, 0, 255); Changed?.Invoke(); }

    /// <summary>Percent the lighten/darken tool changes per stroke step.</summary>
    public int LightenAmount { get; set; } = 10;
    /// <summary>Radius of the spray in pixels and dots per step.</summary>
    public int SprayRadius { get; set; } = 6;
    public int SprayDensity { get; set; } = 6;

    /// <summary>Shading ramp for RGBA sprites without a palette (indexed sprites shade along their palette).</summary>
    public Palette DefaultRamp { get; } = GreyRamp();

    private static Palette GreyRamp()
    {
        var p = new Palette();
        for (int i = 0; i < 8; i++) { byte v = (byte)(i * 255 / 7); p.Colors.Add(new Rgba(v, v, v, 255)); }
        return p;
    }

    /// <summary>Tools whose clicks Alt turns into an eyedropper (the drawing ones).</summary>
    public static bool IsPaintTool(ToolKind k) => k is ToolKind.Pencil or ToolKind.Eraser or ToolKind.Bucket or ToolKind.Line
        or ToolKind.Rectangle or ToolKind.Ellipse or ToolKind.Shading or ToolKind.LightenDarken or ToolKind.Dither or ToolKind.Spray;
}
