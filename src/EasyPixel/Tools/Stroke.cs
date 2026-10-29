using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tools;

/// <summary>
/// One drawing gesture on the current cel, from press to release, recorded as one undo step. It keeps a copy of the
/// cel's pixels from the start (so shape previews and pixel-perfect corners can be put back) and the area touched.
/// A cel that doesn't cover the canvas (files keep cels trimmed) is first grown to cover it; that gesture is then
/// recorded as a whole-cel change (<see cref="CelReplaceEdit"/>) instead of a <see cref="PixelEdit"/>.
/// </summary>
internal sealed class Stroke
{
    // The start-of-stroke copy is reused between strokes: a 4096x4096 sprite would otherwise allocate 64 MB per press.
    private static uint[] _backupBuffer = Array.Empty<uint>();

    private readonly EditorSession _s;
    private readonly string _name;
    private readonly CelState? _grownFrom;
    private readonly PaletteSnapshot? _paletteBefore; // indexed sprites: the palette may grow while drawing
    private readonly CelAddedEdit? _celAdded;           // the layer had no cel in this frame
    private readonly Func<int, int, bool>? _clip;
    private PixelRect _dirty; // image coordinates

    private Stroke(EditorSession s, Cel cel, string name, CelState? grownFrom, PaletteSnapshot? paletteBefore, CelAddedEdit? celAdded)
    {
        _s = s;
        Cel = cel;
        _name = name;
        _grownFrom = grownFrom;
        _paletteBefore = paletteBefore;
        _celAdded = celAdded;
        var px = cel.Image.Pixels;
        if (_backupBuffer.Length < px.Length) _backupBuffer = new uint[px.Length];
        Array.Copy(px, _backupBuffer, px.Length);
        _clip = s.Sel.IsEmpty ? null : s.Sel.AsClip(cel.X, cel.Y);
        var d = s.Doc;
        Symmetry = new Symmetry { H = false, V = false, AxisX = d.Width / 2f - cel.X, AxisY = d.Height / 2f - cel.Y };
    }

    public Cel Cel { get; }
    public Image Image => Cel.Image;
    /// <summary>The mirrors in image coordinates (axes through the canvas centre); null when no mirror is on.</summary>
    public Symmetry? Symmetry { get; private set; }

    /// <summary>Starts a stroke, or reports why the current layer can't be drawn on and returns null.</summary>
    public static Stroke? Begin(ToolContext c, string name)
    {
        var s = c.Session;
        if (s.WhyNotEditable() is { } why) { c.Status(why); return null; }
        s.DropFloating();
        var doc = s.Doc;
        // Colours a stroke adds to an indexed palette (and the transparent entry it may need) belong to its undo step.
        var paletteBefore = doc.Mode == ColorMode.Indexed ? PaletteSnapshot.Take(doc) : null;
        doc.EnsureTransparentIndex();
        var cel = s.CelForDrawing(out var celAdded);
        var old = new CelState(cel.Image, cel.X, cel.Y); // growing replaces the image, so the old one stays as it was
        CelState? grownFrom = cel.GrowToCover(s.CanvasRect, doc.TransparentPixel) ? old : null;
        var st = new Stroke(s, cel, name, grownFrom, paletteBefore, celAdded);
        var t = c.Tools;
        st.Symmetry!.H = t.MirrorH;
        st.Symmetry.V = t.MirrorV;
        if (!t.MirrorH && !t.MirrorV) st.Symmetry = null;
        return st;
    }

    public int ToImageX(int canvasX) => canvasX - Cel.X;
    public int ToImageY(int canvasY) => canvasY - Cel.Y;

    /// <summary>A sink for one step of drawing (image coordinates); hand it back to <see cref="EndStep"/>.</summary>
    public PixelSink Step() => new(Cel.Image, _clip);

    /// <summary>Adds what the step drew to the stroke's area and redraws it on screen.</summary>
    public void EndStep(PixelSink sink, PixelRect alsoChanged = default)
    {
        var d = sink.Dirty.Union(alsoChanged);
        _dirty = _dirty.Union(d);
        _s.Invalidate(ToCanvas(d));
    }

    /// <summary>The stroke's pixel at (x, y) (image coordinates) as it was when the stroke began.</summary>
    public uint Original(int x, int y) =>
        (uint)x < (uint)Image.Width && (uint)y < (uint)Image.Height ? _backupBuffer[y * Image.Width + x] : 0;

    /// <summary>Puts back everything drawn so far (shape tools redraw their preview from scratch on each move).</summary>
    public void RestoreAll()
    {
        if (_dirty.IsEmpty) return;
        Restore(_dirty);
        _s.Invalidate(ToCanvas(_dirty));
    }

    /// <summary>Puts back the original pixels of <paramref name="r"/> (image coordinates).</summary>
    public void Restore(PixelRect r)
    {
        r = r.Intersect(new PixelRect(0, 0, Image.Width, Image.Height));
        for (int y = r.Y; y < r.Bottom; y++)
            Array.Copy(_backupBuffer, y * Image.Width + r.X, Image.Pixels, y * Image.Width + r.X, r.W);
    }

    /// <summary>
    /// Records the stroke as one undo step, together with any palette entries it added (nothing when it drew nothing
    /// and didn't grow the cel).
    /// </summary>
    public void Commit()
    {
        IEdit edit;
        if (_grownFrom != null) edit = CelReplaceEdit.Capture(Cel, _grownFrom, _name);
        else if (_dirty.IsEmpty) { Discard(); return; }
        else
        {
            var r = _dirty.Intersect(new PixelRect(0, 0, Image.Width, Image.Height));
            var before = new uint[r.W * r.H];
            for (int y = 0; y < r.H; y++)
                Array.Copy(_backupBuffer, (r.Y + y) * Image.Width + r.X, before, y * r.W, r.W);
            edit = PixelEdit.Capture(Cel, r, before, _name);
        }
        _s.Push(CompoundEdit.WithPalette(edit, _paletteBefore, _s.Doc, _celAdded));
    }

    /// <summary>Undoes the stroke (and any palette entries it added) without recording anything.</summary>
    public void Cancel()
    {
        RestoreAll();
        Discard();
        if (_grownFrom != null)
        {
            Cel.Image = _grownFrom.Image;
            Cel.X = _grownFrom.X;
            Cel.Y = _grownFrom.Y;
            _s.InvalidateAll();
        }
    }

    /// <summary>Takes back what beginning the stroke changed outside the picture: palette entries, a new cel.</summary>
    private void Discard()
    {
        _paletteBefore?.Restore(_s.Doc);
        _celAdded?.Undo(_s.Doc);
    }

    private PixelRect ToCanvas(PixelRect r) => r.IsEmpty ? r : new PixelRect(r.X + Cel.X, r.Y + Cel.Y, r.W, r.H);
}
