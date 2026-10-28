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
    public required Selection SelectionAtLift { get; init; }
    /// <summary>The selection to put back when the move is cancelled (a paste replaces the selection with its own rectangle).</summary>
    public required Selection SelectionBefore { get; init; }
    public required int StartX { get; init; }
    public required int StartY { get; init; }
    public required string EditName { get; init; }
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
        Layer = doc.Layers.LastOrDefault(l => !l.IsGroup) ?? doc.Layers[0];
        _sel = Selection.None(doc.Width, doc.Height);
    }

    public Sprite Doc { get; }
    public History History { get; }
    public string? Path { get; set; }
    public int Frame { get; set; }
    public Layer Layer { get; set; }

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

    public void Push(IEdit edit)
    {
        History.Push(edit);
        HistoryChanged?.Invoke();
    }

    public bool Undo()
    {
        DropFloating();
        if (!History.Undo()) return false;
        InvalidateAll();
        HistoryChanged?.Invoke();
        return true;
    }

    public bool Redo()
    {
        DropFloating();
        if (!History.Redo()) return false;
        InvalidateAll();
        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Starts moving pixels: <paramref name="f"/> was lifted out of (or will be pasted into) <paramref name="cel"/>.</summary>
    public void BeginFloating(Floating f, Cel cel, CelState before, string editName, Selection? selectionBefore = null)
    {
        Float = new FloatingState
        {
            Pixels = f, Cel = cel, Before = before, SelectionAtLift = Sel, SelectionBefore = selectionBefore ?? Sel, StartX = f.X, StartY = f.Y, EditName = editName,
        };
        Invalidate(Float.Bounds);
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
        Push(CelReplaceEdit.Capture(fs.Cel, fs.Before, fs.EditName));
        _sel = SelectionTools.Translate(fs.SelectionAtLift, fs.Pixels.X - fs.StartX, fs.Pixels.Y - fs.StartY);
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
        _sel = fs.SelectionBefore;
        InvalidateAll();
        SelectionChanged?.Invoke();
    }

    /// <summary>Removes the floating pixels from the picture as one undo step (Delete while moving).</summary>
    public void DeleteFloating()
    {
        if (Float is not { } fs) return;
        Float = null;
        Push(CelReplaceEdit.Capture(fs.Cel, fs.Before, "Delete"));
        _sel = Selection.None(Doc.Width, Doc.Height);
        InvalidateAll();
        SelectionChanged?.Invoke();
    }
}
