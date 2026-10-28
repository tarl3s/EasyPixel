using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>One undoable change. Edits are pushed after the change has been applied.</summary>
public interface IEdit
{
    string Name { get; }
    void Undo(Sprite s);
    void Redo(Sprite s);

    /// <summary>Approximate memory held by this edit, used for the history cap.</summary>
    long Bytes { get; }
}

/// <summary>Linear undo/redo stack with a memory cap.</summary>
public sealed class History
{
    private readonly Sprite _sprite;
    private readonly List<IEdit> _edits = new();
    private readonly List<long> _sizes = new(); // edit.Bytes at push time (an edit's size can change later)
    private int _pos;          // number of applied edits (edits[0.._pos) are applied)
    private int _savedPos;     // position at the last save; -1 when that state is no longer reachable
    private long _bytes;
    private long _maxBytes = 512L * 1024 * 1024;

    public History(Sprite sprite)
    {
        _sprite = sprite;
    }

    /// <summary>Memory cap; the oldest edits are dropped when the total goes over it (the newest applied edit is always kept).</summary>
    public long MaxBytes
    {
        get => _maxBytes;
        set { _maxBytes = value; Trim(); }
    }

    public long TotalBytes => _bytes;
    public int Count => _edits.Count;
    public bool CanUndo => _pos > 0;
    public bool CanRedo => _pos < _edits.Count;
    public string? UndoName => CanUndo ? _edits[_pos - 1].Name : null;
    public string? RedoName => CanRedo ? _edits[_pos].Name : null;

    /// <summary>True when the document differs from the last <see cref="MarkSaved"/> state (or that state was dropped).</summary>
    public bool IsDirty => _savedPos != _pos;

    public void MarkSaved() => _savedPos = _pos;

    public void Push(IEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        if (_pos < _edits.Count)
        {
            for (int i = _pos; i < _edits.Count; i++) _bytes -= _sizes[i];
            _edits.RemoveRange(_pos, _edits.Count - _pos);
            _sizes.RemoveRange(_pos, _sizes.Count - _pos);
            if (_savedPos > _pos) _savedPos = -1; // saved state lived in the discarded redo branch
        }
        _edits.Add(edit);
        long size = edit.Bytes;
        _sizes.Add(size);
        _bytes += size;
        _pos++;
        Trim();
    }

    public bool Undo()
    {
        if (_pos == 0) return false;
        _edits[--_pos].Undo(_sprite);
        return true;
    }

    public bool Redo()
    {
        if (_pos == _edits.Count) return false;
        _edits[_pos++].Redo(_sprite);
        return true;
    }

    private void Trim()
    {
        int drop = 0;
        long bytes = _bytes;
        while (bytes > _maxBytes && drop < _edits.Count - 1 && drop < _pos)
            bytes -= _sizes[drop++];
        if (drop == 0) return;
        _edits.RemoveRange(0, drop);
        _sizes.RemoveRange(0, drop);
        _bytes = bytes;
        _pos -= drop;
        _savedPos = _savedPos - drop < 0 ? -1 : _savedPos - drop;
    }
}
