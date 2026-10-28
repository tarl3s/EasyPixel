using EasyPixel.Core.Model;

namespace EasyPixel.Core.Render;

/// <summary>
/// Reusable work buffers for <see cref="Compositor"/> and <see cref="Onion"/>, so repeated renders of a large sprite
/// do not allocate. Buffers are sprite-sized and replaced only when the size changes; contents are stale between
/// calls (users clear what they draw). Not thread-safe: one instance per thread/owner.
/// </summary>
public sealed class CompositeScratch
{
    [ThreadStatic] private static CompositeScratch? _shared;

    /// <summary>Per-thread instance used when a caller passes none.</summary>
    internal static CompositeScratch Shared => _shared ??= new CompositeScratch();

    private readonly List<Image> _groups = new();
    private Image? _frame;

    /// <summary>Buffer for the group at nesting <paramref name="depth"/>.</summary>
    internal Image Group(int depth, int w, int h)
    {
        while (_groups.Count <= depth) _groups.Add(new Image(1, 1));
        return _groups[depth] = Fit(_groups[depth], w, h);
    }

    /// <summary>Buffer holding one flattened ghost frame.</summary>
    internal Image Frame(int w, int h) => _frame = Fit(_frame, w, h);

    private static Image Fit(Image? img, int w, int h) => img != null && img.Width == w && img.Height == h ? img : new Image(w, h);
}
