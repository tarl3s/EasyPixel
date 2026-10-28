namespace EasyPixel.Core.Model;

/// <summary>
/// Layer list operations that keep groups whole. <see cref="Sprite.Layers"/> is kept as Aseprite stores it: bottom to
/// top, each group followed by its members (a "block"), so a group's block moves as one piece. None of these record
/// undo steps; wrap them in a LayerStructureEdit.
/// </summary>
public static class LayerTree
{
    /// <summary>Keys of <see cref="Layer.Extra"/> that identify one layer in a file and must not be copied to another.</summary>
    public static readonly string[] IdentityKeys = { "ase:id", "ase:uuid" };

    public static bool IsInside(Layer layer, Layer group)
    {
        for (var p = layer.Parent; p != null; p = p.Parent)
            if (p == group) return true;
        return false;
    }

    /// <summary>The layer and everything inside it, in list order.</summary>
    public static List<Layer> Block(Sprite s, Layer layer) =>
        s.Layers.Where(l => l == layer || IsInside(l, layer)).ToList();

    /// <summary>The layers with the same parent, bottom to top.</summary>
    public static List<Layer> Siblings(Sprite s, Layer layer) => s.Layers.Where(l => l.Parent == layer.Parent).ToList();

    /// <summary>Takes the layer's block out of the list and returns it.</summary>
    private static List<Layer> Extract(Sprite s, Layer layer)
    {
        var block = Block(s, layer);
        foreach (var l in block) s.Layers.Remove(l);
        return block;
    }

    /// <summary>Index just after the block of <paramref name="layer"/>.</summary>
    private static int EndOf(Sprite s, Layer layer) => s.Layers.IndexOf(Block(s, layer)[^1]) + 1;

    /// <summary>Adds a new layer (or group) right above <paramref name="above"/> in its group; on top when null.</summary>
    public static void Insert(Sprite s, Layer layer, Layer? above)
    {
        if (above == null) { layer.Parent = null; s.Layers.Add(layer); return; }
        layer.Parent = above.Parent;
        s.Layers.Insert(EndOf(s, above), layer);
    }

    /// <summary>Removes the layer, everything inside it and their cels. Returns false (and does nothing) for the last drawable layer.</summary>
    public static bool Remove(Sprite s, Layer layer)
    {
        var block = Block(s, layer);
        if (!s.Layers.Except(block).Any(l => !l.IsGroup)) return false;
        foreach (var key in s.Cels.Keys.Where(k => block.Contains(k.Item1)).ToList()) s.Cels.Remove(key);
        foreach (var l in block) s.Layers.Remove(l);
        return true;
    }

    /// <summary>Moves the layer (with its members) one step up (+1) or down (-1) among its siblings. False when it can't move.</summary>
    public static bool Move(Sprite s, Layer layer, int direction)
    {
        var sib = Siblings(s, layer);
        int i = sib.IndexOf(layer), j = i + Math.Sign(direction);
        if (direction == 0 || j < 0 || j >= sib.Count) return false;
        var other = sib[j];
        var block = Extract(s, layer);
        int at = direction > 0 ? EndOf(s, other) : s.Layers.IndexOf(other);
        s.Layers.InsertRange(at, block);
        return true;
    }

    /// <summary>Puts the layer in a new group (in the layer's place) and returns the group.</summary>
    public static Layer Group(Sprite s, Layer layer, string name)
    {
        var group = new Layer { Name = name, IsGroup = true, Parent = layer.Parent };
        s.Layers.Insert(s.Layers.IndexOf(layer), group);
        layer.Parent = group;
        return group;
    }

    /// <summary>Moves the layer into <paramref name="group"/>, on top of its members. False when that would put a group inside itself.</summary>
    public static bool PutInto(Sprite s, Layer layer, Layer group)
    {
        if (!group.IsGroup || group == layer || IsInside(group, layer)) return false;
        var block = Extract(s, layer);
        s.Layers.InsertRange(EndOf(s, group), block);
        layer.Parent = group;
        return true;
    }

    /// <summary>Moves the layer out of its group, right above the group. False when it is not in a group.</summary>
    public static bool MoveOut(Sprite s, Layer layer)
    {
        if (layer.Parent is not { } group) return false;
        var block = Extract(s, layer);
        s.Layers.InsertRange(EndOf(s, group), block);
        layer.Parent = group.Parent;
        return true;
    }

    /// <summary>
    /// Copies the layer (a group with all its members) right above it, cels included (linked cels stay linked in the
    /// copy). The copy keeps the format data in <c>Extra</c> except the keys that identify the original
    /// (<see cref="IdentityKeys"/>), so a file never gets two layers with one id. Returns the copy of <paramref name="layer"/>.
    /// </summary>
    public static Layer Duplicate(Sprite s, Layer layer, string nameSuffix)
    {
        var block = Block(s, layer);
        var map = new Dictionary<Layer, Layer>();
        foreach (var l in block)
        {
            var c = new Layer
            {
                Name = l == layer ? l.Name + nameSuffix : l.Name, Visible = l.Visible, Locked = l.Locked, Opacity = l.Opacity,
                Blend = l.Blend, IsGroup = l.IsGroup, Parent = l.Parent != null && map.TryGetValue(l.Parent, out var p) ? p : l.Parent,
            };
            foreach (var kv in l.Extra)
                if (!IdentityKeys.Contains(kv.Key)) c.Extra[kv.Key] = (byte[])kv.Value.Clone();
            map[l] = c;
        }
        var cels = new Dictionary<Cel, Cel>();
        foreach (var kv in s.Cels.Where(kv => map.ContainsKey(kv.Key.Item1)).ToList())
        {
            if (!cels.TryGetValue(kv.Value, out var copy))
            {
                var o = kv.Value;
                copy = new Cel(o.Image.Clone(), o.X, o.Y) { Opacity = o.Opacity };
                foreach (var e in o.Extra) copy.Extra[e.Key] = (byte[])e.Value.Clone();
                cels[o] = copy;
            }
            s.Cels[(map[kv.Key.Item1], kv.Key.Item2)] = copy;
        }
        s.Layers.InsertRange(EndOf(s, layer), block.Select(l => map[l]));
        return map[layer];
    }
}
