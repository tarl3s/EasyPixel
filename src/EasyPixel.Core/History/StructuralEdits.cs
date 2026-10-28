using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

// Each Perform() applies the change to the sprite and returns the edit to push.
// Not covered: tags (their frame ranges are not adjusted) and group children of a removed layer.

public sealed class AddLayerEdit : IEdit
{
    private readonly Layer _layer;
    private readonly int _index;

    private AddLayerEdit(Layer layer, int index) { _layer = layer; _index = index; }

    public string Name => "Add layer";
    public long Bytes => 64;

    public static AddLayerEdit Perform(Sprite s, Layer layer, int index)
    {
        s.Layers.Insert(index, layer);
        return new AddLayerEdit(layer, index);
    }

    public void Undo(Sprite s) => s.Layers.Remove(_layer);
    public void Redo(Sprite s) => s.Layers.Insert(_index, _layer);
}

public sealed class RemoveLayerEdit : IEdit
{
    private readonly Layer _layer;
    private readonly int _index;
    private readonly List<(int Frame, Cel Cel)> _cels;

    private RemoveLayerEdit(Layer layer, int index, List<(int Frame, Cel Cel)> cels) { _layer = layer; _index = index; _cels = cels; }

    public string Name => "Remove layer";
    public long Bytes => 64 + _cels.Sum(c => c.Cel.Image.Pixels.Length * 4L);

    public static RemoveLayerEdit Perform(Sprite s, int index)
    {
        var layer = s.Layers[index];
        var cels = s.Cels.Where(kv => kv.Key.Item1 == layer).Select(kv => (Frame: kv.Key.Item2, Cel: kv.Value)).ToList();
        foreach (var c in cels) s.Cels.Remove((layer, c.Frame));
        s.Layers.RemoveAt(index);
        return new RemoveLayerEdit(layer, index, cels);
    }

    public void Undo(Sprite s)
    {
        s.Layers.Insert(_index, _layer);
        foreach (var c in _cels) s.Cels[(_layer, c.Frame)] = c.Cel;
    }

    public void Redo(Sprite s)
    {
        foreach (var c in _cels) s.Cels.Remove((_layer, c.Frame));
        s.Layers.Remove(_layer);
    }
}

public sealed class AddFrameEdit : IEdit
{
    private readonly Frame _frame;
    private readonly int _index;

    private AddFrameEdit(Frame frame, int index) { _frame = frame; _index = index; }

    public string Name => "Add frame";
    public long Bytes => 64;

    public static AddFrameEdit Perform(Sprite s, Frame frame, int index)
    {
        FrameShift.Open(s, index);
        s.Frames.Insert(index, frame);
        return new AddFrameEdit(frame, index);
    }

    public void Undo(Sprite s) { s.Frames.RemoveAt(_index); FrameShift.Close(s, _index); }
    public void Redo(Sprite s) { FrameShift.Open(s, _index); s.Frames.Insert(_index, _frame); }
}

public sealed class RemoveFrameEdit : IEdit
{
    private readonly Frame _frame;
    private readonly int _index;
    private readonly List<(Layer Layer, Cel Cel)> _cels;

    private RemoveFrameEdit(Frame frame, int index, List<(Layer Layer, Cel Cel)> cels) { _frame = frame; _index = index; _cels = cels; }

    public string Name => "Remove frame";
    public long Bytes => 64 + _cels.Sum(c => c.Cel.Image.Pixels.Length * 4L);

    public static RemoveFrameEdit Perform(Sprite s, int index)
    {
        var frame = s.Frames[index];
        var cels = s.Cels.Where(kv => kv.Key.Item2 == index).Select(kv => (Layer: kv.Key.Item1, Cel: kv.Value)).ToList();
        foreach (var c in cels) s.Cels.Remove((c.Layer, index));
        s.Frames.RemoveAt(index);
        FrameShift.Close(s, index);
        return new RemoveFrameEdit(frame, index, cels);
    }

    public void Undo(Sprite s)
    {
        FrameShift.Open(s, _index);
        s.Frames.Insert(_index, _frame);
        foreach (var c in _cels) s.Cels[(c.Layer, _index)] = c.Cel;
    }

    public void Redo(Sprite s)
    {
        foreach (var c in _cels) s.Cels.Remove((c.Layer, _index));
        s.Frames.RemoveAt(_index);
        FrameShift.Close(s, _index);
    }
}

/// <summary>Moves the cel keys of frames at or after an index up (a gap opens) or down (a gap closes).</summary>
internal static class FrameShift
{
    public static void Open(Sprite s, int index) => Move(s, index, +1);
    public static void Close(Sprite s, int index) => Move(s, index + 1, -1);

    private static void Move(Sprite s, int from, int delta)
    {
        var moved = s.Cels.Where(kv => kv.Key.Item2 >= from).ToList();
        foreach (var kv in moved) s.Cels.Remove(kv.Key);
        foreach (var kv in moved) s.Cels[(kv.Key.Item1, kv.Key.Item2 + delta)] = kv.Value;
    }
}

public sealed class ResizeEdit : IEdit
{
    private readonly int _oldW, _oldH, _newW, _newH;
    private readonly List<(Cel Cel, int OldX, int OldY, int NewX, int NewY)> _cels;

    private ResizeEdit(int oldW, int oldH, int newW, int newH, List<(Cel Cel, int OldX, int OldY, int NewX, int NewY)> cels)
    {
        _oldW = oldW; _oldH = oldH; _newW = newW; _newH = newH; _cels = cels;
    }

    public string Name => "Resize canvas";
    public long Bytes => 64 + _cels.Count * 24L;

    public static ResizeEdit Perform(Sprite s, int width, int height, Anchor anchor)
    {
        int oldW = s.Width, oldH = s.Height;
        var before = s.Cels.Values.Select(c => (Cel: c, c.X, c.Y)).ToList();
        s.Resize(width, height, anchor);
        var cels = before.Select(b => (b.Cel, b.X, b.Y, b.Cel.X, b.Cel.Y)).ToList();
        return new ResizeEdit(oldW, oldH, width, height, cels);
    }

    public void Undo(Sprite s)
    {
        s.SetCanvasSize(_oldW, _oldH);
        foreach (var c in _cels) { c.Cel.X = c.OldX; c.Cel.Y = c.OldY; }
    }

    public void Redo(Sprite s)
    {
        s.SetCanvasSize(_newW, _newH);
        foreach (var c in _cels) { c.Cel.X = c.NewX; c.Cel.Y = c.NewY; }
    }
}
