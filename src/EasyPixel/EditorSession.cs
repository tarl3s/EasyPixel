using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel;

/// <summary>Pixels lifted (or pasted) and being moved: dropped into <see cref="Cel"/> by <see cref="EditorSession.DropFloating"/>.</summary>
public sealed class FloatingState
{
    public required Floating Pixels { get; init; }
    public required Cel Cel { get; init; }
    /// <summary>The cel before the lift or paste, for the undo step and for cancelling.</summary>
    public required CelState Before { get; init; }
    /// <summary>The selection when the pixels were lifted, and where the pixels were then.</summary>
    public required Selection SelectionAtLift { get; set; }
    /// <summary>The selection to put back when the move is cancelled (a paste replaces the selection with its own rectangle).</summary>
    public required Selection SelectionBefore { get; init; }
    public required int StartX { get; set; }
    public required int StartY { get; set; }
    public required string EditName { get; init; }
    /// <summary>Indexed sprites: the palette before the lift or paste (a paste may add colours), so they share its undo step.</summary>
    public PaletteSnapshot? PaletteBefore { get; init; }
    /// <summary>The cel had to be made for the lift or paste: its creation joins the undo step.</summary>
    public CelAddedEdit? CelAdded { get; init; }
    /// <summary>Bumped whenever the pixels themselves change (not their position), so views can cache a picture of them.</summary>
    public int Version { get; set; }
    public PixelRect Bounds => new(Pixels.X, Pixels.Y, Pixels.Pixels.Width, Pixels.Pixels.Height);
}

/// <summary>One open document and where the person is in it: the sprite, its undo history, file, frame, layer and selection.</summary>
public sealed class EditorSession
{
    private Selection _sel;

    public EditorSession(Sprite doc, string? path = null)
    {
        Doc = doc;
        History = new History(doc);
        Path = path;
        _layer = doc.Layers.LastOrDefault(l => !l.IsGroup) ?? doc.Layers[0];
        _sel = Selection.None(doc.Width, doc.Height);
    }

    public Sprite Doc { get; }
    public History History { get; }
    public string? Path { get; set; }

    /// <summary>The frame being edited (0-based).</summary>
    public int Frame
    {
        get => _frame;
        set
        {
            value = Math.Clamp(value, 0, Doc.Frames.Count - 1);
            if (value == _frame) return;
            DropFloating();
            _frame = value;
            CurrentChanged?.Invoke();
            InvalidateAll();
        }
    }
    private int _frame;

    /// <summary>The layer being edited (a group can be current; drawing on it is refused).</summary>
    public Layer Layer
    {
        get => _layer;
        set
        {
            if (value == _layer || !Doc.Layers.Contains(value)) return;
            DropFloating();
            _layer = value;
            CurrentChanged?.Invoke();
        }
    }
    private Layer _layer;

    /// <summary>The current frame or layer changed.</summary>
    public event Action? CurrentChanged;

    /// <summary>Selected canvas pixels; empty when nothing is selected.</summary>
    public Selection Sel
    {
        get => _sel;
        set { _sel = value; SelectionChanged?.Invoke(); }
    }

    /// <summary>Pixels being moved, or null.</summary>
    public FloatingState? Float { get; private set; }

    /// <summary>A canvas area (sprite coordinates) changed and must be redrawn.</summary>
    public event Action<PixelRect>? Changed;
    public event Action? SelectionChanged;
    /// <summary>The history changed (an edit was pushed, undone or redone): titles and menus may need an update.</summary>
    public event Action? HistoryChanged;

    public PixelRect CanvasRect => new(0, 0, Doc.Width, Doc.Height);

    /// <summary>The file name to show, or "Untitled".</summary>
    public string DisplayName => Path == null ? L.T("Untitled") : System.IO.Path.GetFileName(Path);

    public void Invalidate(PixelRect canvasArea) { if (!canvasArea.IsEmpty) Changed?.Invoke(canvasArea); }
    public void InvalidateAll() => Changed?.Invoke(CanvasRect);

    /// <summary>Why the current layer can't be drawn on, or null when it can.</summary>
    public string? WhyNotEditable()
    {
        if (Layer.IsGroup) return L.T("A group can't be drawn on: pick a layer.");
        if (Layer.Locked) return L.T("The layer is locked.");
        if (!Layer.Visible) return L.T("The layer is hidden.");
        return null;
    }

    /// <summary>The current layer's cel in the current frame, made when there is none.</summary>
    public Cel CelForDrawing() => Doc.GetOrCreateCel(Layer, Frame);

    /// <summary>As <see cref="CelForDrawing()"/>; <paramref name="added"/> is the undo step for a cel that had to be made (join it to the gesture's step).</summary>
    public Cel CelForDrawing(out CelAddedEdit? added)
    {
        bool existed = Doc.Cels.ContainsKey((Layer, Frame));
        var cel = CelForDrawing();
        added = existed ? null : new CelAddedEdit(Layer, Frame, cel);
        return cel;
    }

    public void Push(IEdit edit)
    {
        History.Push(edit);
        HistoryChanged?.Invoke();
    }

    /// <summary>Records a change that was already made to the document (a panel or menu command) and redraws everything.</summary>
    public void Apply(IEdit edit)
    {
        History.Push(edit);
        KeepCurrentValid();
        InvalidateAll();
        HistoryChanged?.Invoke();
    }

    /// <summary>
    /// After a change to the document's structure (or undoing one): the current frame and layer must still exist, and
    /// the selection must have the canvas size.
    /// </summary>
    private void KeepCurrentValid()
    {
        bool changed = false;
        if (_frame >= Doc.Frames.Count) { _frame = Doc.Frames.Count - 1; changed = true; }
        if (!Doc.Layers.Contains(_layer)) { _layer = Doc.Layers.LastOrDefault(l => !l.IsGroup) ?? Doc.Layers[0]; changed = true; }
        if (_sel.Width != Doc.Width || _sel.Height != Doc.Height) Sel = Selection.None(Doc.Width, Doc.Height);
        if (changed) CurrentChanged?.Invoke();
    }

    public bool Undo()
    {
        DropFloating();
        if (!History.Undo()) return false;
        KeepCurrentValid();
        InvalidateAll();
        HistoryChanged?.Invoke();
        return true;
    }

    public bool Redo()
    {
        DropFloating();
        if (!History.Redo()) return false;
        KeepCurrentValid();
        InvalidateAll();
        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Starts moving pixels: <paramref name="f"/> was lifted out of (or will be pasted into) <paramref name="cel"/>.</summary>
    public void BeginFloating(Floating f, Cel cel, CelState before, string editName, Selection? selectionBefore = null, PaletteSnapshot? paletteBefore = null,
        CelAddedEdit? celAdded = null)
    {
        Float = new FloatingState
        {
            Pixels = f, Cel = cel, Before = before, SelectionAtLift = Sel, SelectionBefore = selectionBefore ?? Sel, StartX = f.X, StartY = f.Y, EditName = editName, PaletteBefore = paletteBefore, CelAdded = celAdded,
        };
        Invalidate(Float.Bounds);
        SelectionChanged?.Invoke();
    }

    /// <summary>Lifts the selected pixels of the current layer so they float (see <see cref="Float"/>).</summary>
    public void LiftSelection(string editName)
    {
        var paletteBefore = Doc.Mode == ColorMode.Indexed ? PaletteSnapshot.Take(Doc) : null;
        Doc.EnsureTransparentIndex(); // the hole left behind must be transparent
        var cel = CelForDrawing(out var added);
        var before = CelReplaceEdit.Begin(cel);
        var f = Floating.Lift(cel, Sel, Doc.TransparentPixel);
        BeginFloating(f, cel, before, editName, paletteBefore: paletteBefore, celAdded: added);
    }

    /// <summary>
    /// Flips, turns or scales the floating pixels (<paramref name="change"/> runs on them). The selection outline gets the
    /// same change, so it keeps following the pixels; the whole move stays one undo step.
    /// </summary>
    public void TransformFloating(Action<Floating> change)
    {
        if (Float is not { } fs) return;
        var old = fs.Bounds;
        var img = fs.Pixels.Pixels;
        var mask = new Floating(new Image(img.Width, img.Height), fs.Pixels.X, fs.Pixels.Y);
        int dx = fs.StartX - fs.Pixels.X, dy = fs.StartY - fs.Pixels.Y;
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (fs.SelectionAtLift.Contains(mask.X + x + dx, mask.Y + y + dy)) mask.Pixels.Pixels[y * img.Width + x] = 1;
        change(fs.Pixels);
        change(mask);
        var sel = new Selection(Doc.Width, Doc.Height);
        var m = mask.Pixels;
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
                if (m.Pixels[y * m.Width + x] != 0) sel.Set(mask.X + x, mask.Y + y, true);
        fs.SelectionAtLift = sel;
        fs.StartX = fs.Pixels.X;
        fs.StartY = fs.Pixels.Y;
        fs.Version++;
        Invalidate(old.Union(fs.Bounds));
        SelectionChanged?.Invoke();
    }

    /// <summary>Moves the floating pixels to (x, y).</summary>
    public void MoveFloating(int x, int y)
    {
        if (Float == null) return;
        var old = Float.Bounds;
        Float.Pixels.X = x;
        Float.Pixels.Y = y;
        Invalidate(old.Union(Float.Bounds));
    }

    /// <summary>Writes the floating pixels into their cel as one undo step; the selection follows them.</summary>
    public void DropFloating()
    {
        if (Float is not { } fs) return;
        Float = null;
        fs.Pixels.Drop(fs.Cel);
        Push(CompoundEdit.WithPalette(CelReplaceEdit.Capture(fs.Cel, fs.Before, fs.EditName), fs.PaletteBefore, Doc, fs.CelAdded));
        _sel = fs.SelectionAtLift.Offset(fs.Pixels.X - fs.StartX, fs.Pixels.Y - fs.StartY);
        Invalidate(fs.Bounds);
        SelectionChanged?.Invoke();
    }

    /// <summary>Throws the floating pixels away: a lift puts them back, a paste never happened.</summary>
    public void CancelFloating()
    {
        if (Float is not { } fs) return;
        Float = null;
        fs.Cel.Image = fs.Before.Image;
        fs.Cel.X = fs.Before.X;
        fs.Cel.Y = fs.Before.Y;
        fs.PaletteBefore?.Restore(Doc);
        fs.CelAdded?.Undo(Doc);
        _sel = fs.SelectionBefore;
        InvalidateAll();
        SelectionChanged?.Invoke();
    }

    /// <summary>Removes the floating pixels from the picture as one undo step (Delete while moving).</summary>
    public void DeleteFloating()
    {
        if (Float is not { } fs) return;
        Float = null;
        Push(CompoundEdit.WithPalette(CelReplaceEdit.Capture(fs.Cel, fs.Before, "Delete"), fs.PaletteBefore, Doc, fs.CelAdded));
        _sel = Selection.None(Doc.Width, Doc.Height);
        InvalidateAll();
        SelectionChanged?.Invoke();
    }
}
