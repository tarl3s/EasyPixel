using EasyPixel.Core.Model;

namespace EasyPixel.Tools;

/// <summary>One pointer event in canvas pixels, with the button and the modifier keys.</summary>
public readonly record struct ToolInput(int X, int Y, bool Right = false, bool Shift = false, bool Alt = false, bool Ctrl = false);

/// <summary>What a tool works on and reports to.</summary>
public sealed class ToolContext
{
    public ToolContext(EditorSession session, ToolController tools, Func<int, int, Rgba> pickComposite, Action<string> status)
    {
        Session = session;
        Tools = tools;
        PickComposite = pickComposite;
        Status = status;
    }

    public EditorSession Session { get; }
    public ToolController Tools { get; }
    /// <summary>The colour shown at a canvas pixel (all visible layers).</summary>
    public Func<int, int, Rgba> PickComposite { get; }
    /// <summary>Shows a short message to the person (e.g. why nothing was drawn).</summary>
    public Action<string> Status { get; }
}

public enum OverlayShape { Rect, Ellipse, Polyline }

/// <summary>A guide the canvas draws for a tool in progress (a marquee being dragged, a lasso path), in canvas pixels.</summary>
public sealed record ToolOverlay(OverlayShape Shape, IReadOnlyList<(int X, int Y)> Points);

/// <summary>A tool: gets pointer events in canvas pixels between <see cref="Down"/> and <see cref="Up"/>.</summary>
public abstract class Tool
{
    protected Tool(ToolKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    public ToolKind Kind { get; }
    /// <summary>English name (shown through L.T).</summary>
    public string Name { get; }

    /// <summary>True between Down and Up.</summary>
    public bool Active { get; protected set; }

    /// <summary>The guide to draw, or null.</summary>
    public virtual ToolOverlay? Overlay => null;

    public abstract void Down(ToolContext c, ToolInput p);
    public virtual void Move(ToolContext c, ToolInput p) { }
    public virtual void Up(ToolContext c, ToolInput p) { Active = false; }

    /// <summary>Abandons a gesture in progress (Escape, losing the pointer).</summary>
    public virtual void Cancel(ToolContext c) { Active = false; }

    /// <summary>The pixel value a press draws with: the primary colour, or the secondary with the right button.</summary>
    protected static uint PaintValue(ToolContext c, ToolInput p) =>
        c.Session.Doc.ColorToPixel(p.Right ? c.Tools.Secondary : c.Tools.Primary);
}
