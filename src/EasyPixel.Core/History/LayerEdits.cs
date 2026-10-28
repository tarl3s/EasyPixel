using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>The layer list with each layer's group, so a structural change can be put back exactly.</summary>
public sealed class LayerOrder
{
    internal readonly List<(Layer Layer, Layer? Parent)> Items;
    private LayerOrder(List<(Layer, Layer?)> items) { Items = items; }

    public static LayerOrder Take(Sprite s) => new(s.Layers.Select(l => (l, l.Parent)).ToList());

    internal void Restore(Sprite s)
    {
        s.Layers.Clear();
        foreach (var (l, p) in Items) { l.Parent = p; s.Layers.Add(l); }
    }

    internal HashSet<Layer> Layers => Items.Select(i => i.Layer).ToHashSet();
}

/// <summary>
/// Any change to which layers exist, their order or their groups (add, remove, duplicate, move, group, ungroup): the
/// list is recorded before and after; the cels of layers that only exist on one side are held while they are gone.
/// Use with the <see cref="LayerTree"/> operations: <c>var b = LayerOrder.Take(s); LayerTree.X(s, …); Push(LayerStructureEdit.Capture(s, b, name))</c>.
/// </summary>
public sealed class LayerStructureEdit : IEdit
{
    private readonly LayerOrder _before, _after;
    private readonly HashSet<Layer> _added, _removed;
    private List<KeyValuePair<(Layer, int), Cel>> _addedCels = new();   // held while undone
    private List<KeyValuePair<(Layer, int), Cel>> _removedCels;         // held while done

    private LayerStructureEdit(string name, LayerOrder before, LayerOrder after, HashSet<Layer> added, HashSet<Layer> removed,
        List<KeyValuePair<(Layer, int), Cel>> removedCels)
    {
        Name = name; _before = before; _after = after; _added = added; _removed = removed; _removedCels = removedCels;
    }

    public string Name { get; }

    public long Bytes => 64 + _before.Items.Count * 16L + _after.Items.Count * 16L
        + _addedCels.Concat(_removedCels).Select(kv => kv.Value).Distinct().Sum(c => c.Image.Pixels.Length * 4L);

    /// <summary>
    /// Records the change from <paramref name="before"/> to the list as it is now. Layers that were removed must already
    /// have lost their cels: pass them in <paramref name="removedCels"/> (from <see cref="CelsOf"/> taken before removing).
    /// </summary>
    public static LayerStructureEdit Capture(Sprite s, LayerOrder before, string name, List<KeyValuePair<(Layer, int), Cel>>? removedCels = null)
    {
        var after = LayerOrder.Take(s);
        var b = before.Layers;
        var a = after.Layers;
        var added = a.Where(l => !b.Contains(l)).ToHashSet();
        var removed = b.Where(l => !a.Contains(l)).ToHashSet();
        return new LayerStructureEdit(name, before, after, added, removed,
            removedCels?.Where(kv => removed.Contains(kv.Key.Item1)).ToList() ?? new());
    }

    /// <summary>The cels of the given layers (to pass to <see cref="Capture"/> when they are about to be removed).</summary>
    public static List<KeyValuePair<(Layer, int), Cel>> CelsOf(Sprite s, IEnumerable<Layer> layers)
    {
        var set = layers.ToHashSet();
        return s.Cels.Where(kv => set.Contains(kv.Key.Item1)).ToList();
    }

    /// <summary>Runs <paramref name="change"/> (which may remove layers with their cels) and records it.</summary>
    public static LayerStructureEdit Perform(Sprite s, string name, Action change)
    {
        var before = LayerOrder.Take(s);
        var cels = s.Cels.ToList();
        change();
        return Capture(s, before, name, cels);
    }

    public void Undo(Sprite s)
    {
        _addedCels = Take(s, _added);
        _before.Restore(s);
        foreach (var kv in _removedCels) s.Cels[kv.Key] = kv.Value;
    }

    public void Redo(Sprite s)
    {
        _removedCels = Take(s, _removed);
        _after.Restore(s);
        foreach (var kv in _addedCels) s.Cels[kv.Key] = kv.Value;
        _addedCels = new();
    }

    private static List<KeyValuePair<(Layer, int), Cel>> Take(Sprite s, HashSet<Layer> layers)
    {
        var cels = s.Cels.Where(kv => layers.Contains(kv.Key.Item1)).ToList();
        foreach (var kv in cels) s.Cels.Remove(kv.Key);
        return cels;
    }
}

/// <summary>A layer's own settings: name, visibility, lock, opacity and blend mode.</summary>
public readonly record struct LayerProps(string Name, bool Visible, bool Locked, byte Opacity, BlendMode Blend)
{
    public static LayerProps Of(Layer l) => new(l.Name, l.Visible, l.Locked, l.Opacity, l.Blend);

    public void ApplyTo(Layer l)
    {
        l.Name = Name; l.Visible = Visible; l.Locked = Locked; l.Opacity = Opacity; l.Blend = Blend;
    }
}

/// <summary>A change of a layer's settings (rename, show/hide, lock, opacity, blend mode).</summary>
public sealed class LayerPropsEdit : IEdit
{
    private readonly Layer _layer;
    private readonly LayerProps _before, _after;

    private LayerPropsEdit(string name, Layer layer, LayerProps before, LayerProps after)
    {
        Name = name; _layer = layer; _before = before; _after = after;
    }

    public string Name { get; }
    public long Bytes => 64 + (_before.Name.Length + _after.Name.Length) * 2L;

    /// <summary>Applies <paramref name="after"/> to the layer and records the change.</summary>
    public static LayerPropsEdit Perform(Layer layer, LayerProps after, string name)
    {
        var before = LayerProps.Of(layer);
        after.ApplyTo(layer);
        return new LayerPropsEdit(name, layer, before, after);
    }

    public void Undo(Sprite s) => _before.ApplyTo(_layer);
    public void Redo(Sprite s) => _after.ApplyTo(_layer);
}
