using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>Moves a frame (its cels and duration) to another position; tags keep their frame numbers.</summary>
public sealed class MoveFrameEdit : IEdit
{
    private readonly int _from, _to;

    private MoveFrameEdit(int from, int to) { _from = from; _to = to; }

    public string Name => "Move frame";
    public long Bytes => 64;

    public static MoveFrameEdit Perform(Sprite s, int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(from, s.Frames.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(to);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(to, s.Frames.Count);
        Move(s, from, to);
        return new MoveFrameEdit(from, to);
    }

    public void Undo(Sprite s) => Move(s, _to, _from);
    public void Redo(Sprite s) => Move(s, _from, _to);

    private static void Move(Sprite s, int from, int to)
    {
        if (from == to) return;
        int Map(int i) => i == from ? to : from < to ? (i > from && i <= to ? i - 1 : i) : (i >= to && i < from ? i + 1 : i);
        var frame = s.Frames[from];
        s.Frames.RemoveAt(from);
        s.Frames.Insert(to, frame);
        int lo = Math.Min(from, to), hi = Math.Max(from, to);
        var moved = s.Cels.Where(kv => kv.Key.Item2 >= lo && kv.Key.Item2 <= hi).ToList();
        foreach (var kv in moved) s.Cels.Remove(kv.Key);
        foreach (var kv in moved) s.Cels[(kv.Key.Item1, Map(kv.Key.Item2))] = kv.Value;
    }
}

/// <summary>New durations for some frames.</summary>
public sealed class FrameDurationEdit : IEdit
{
    private readonly List<(Frame Frame, int Before, int After)> _frames;

    private FrameDurationEdit(List<(Frame, int, int)> frames) { _frames = frames; }

    public string Name => "Frame duration";
    public long Bytes => 64 + _frames.Count * 16L;

    /// <summary>Sets every frame in <paramref name="indices"/> to <paramref name="ms"/> milliseconds (at least 1).</summary>
    public static FrameDurationEdit Perform(Sprite s, IEnumerable<int> indices, int ms)
    {
        ms = Math.Max(1, ms);
        var list = indices.Distinct().Select(i => s.Frames[i]).Select(f => (f, f.DurationMs, ms)).ToList();
        foreach (var (f, _, after) in list) f.DurationMs = after;
        return new FrameDurationEdit(list);
    }

    public void Undo(Sprite s) { foreach (var (f, before, _) in _frames) f.DurationMs = before; }
    public void Redo(Sprite s) { foreach (var (f, _, after) in _frames) f.DurationMs = after; }
}

/// <summary>Frame operations that need more than one step.</summary>
public static class FrameOps
{
    /// <summary>
    /// Inserts a copy of frame <paramref name="index"/> right after it (same duration, copies of its cels; cels linked to
    /// each other stay linked in the copy) and returns the undo step.
    /// </summary>
    public static AddFrameEdit Duplicate(Sprite s, int index)
    {
        var src = s.Frames[index];
        var copy = new Frame { DurationMs = src.DurationMs };
        var cels = s.Cels.Where(kv => kv.Key.Item2 == index).ToList();
        var edit = AddFrameEdit.Perform(s, copy, index + 1);
        var copies = new Dictionary<Cel, Cel>();
        foreach (var kv in cels)
        {
            if (!copies.TryGetValue(kv.Value, out var c))
            {
                var o = kv.Value;
                c = new Cel(o.Image.Clone(), o.X, o.Y) { Opacity = o.Opacity };
                foreach (var e in o.Extra) c.Extra[e.Key] = (byte[])e.Value.Clone();
                copies[o] = c;
            }
            s.Cels[(kv.Key.Item1, index + 1)] = c;
        }
        return edit;
    }
}

/// <summary>A tag's settings at one moment.</summary>
public readonly record struct TagState(Tag Tag, string Name, int From, int To, LoopDir Dir, Rgba Color)
{
    public static TagState Of(Tag t) => new(t, t.Name, t.From, t.To, t.Dir, t.Color);

    public void Restore()
    {
        Tag.Name = Name; Tag.From = From; Tag.To = To; Tag.Dir = Dir; Tag.Color = Color;
    }
}

/// <summary>Any change to the tags (create, remove, rename, range, direction, colour): the whole list before and after.</summary>
public sealed class TagsEdit : IEdit
{
    private readonly List<TagState> _before, _after;

    private TagsEdit(string name, List<TagState> before, List<TagState> after) { Name = name; _before = before; _after = after; }

    public string Name { get; }
    public long Bytes => 64 + (_before.Count + _after.Count) * 48L;

    public static List<TagState> Take(Sprite s) => s.Tags.Select(TagState.Of).ToList();

    /// <summary>Records the change from <paramref name="before"/> (<see cref="Take"/>) to the tags as they are now.</summary>
    public static TagsEdit Capture(Sprite s, List<TagState> before, string name) => new(name, before, Take(s));

    public void Undo(Sprite s) => Restore(s, _before);
    public void Redo(Sprite s) => Restore(s, _after);

    private static void Restore(Sprite s, List<TagState> states)
    {
        s.Tags.Clear();
        foreach (var t in states) { t.Restore(); s.Tags.Add(t.Tag); }
    }
}
