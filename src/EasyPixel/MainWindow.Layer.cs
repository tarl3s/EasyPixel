using Avalonia.Interactivity;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;

namespace EasyPixel;

// Layer menu and the layers panel's commands. Each change is one undo step.
public partial class MainWindow
{
    private void NewLayer_Click(object? sender, RoutedEventArgs e) => NewLayer();
    private void NewGroup_Click(object? sender, RoutedEventArgs e) => NewGroup();
    private void DuplicateLayer_Click(object? sender, RoutedEventArgs e) => DuplicateLayer();
    private void RemoveLayer_Click(object? sender, RoutedEventArgs e) => RemoveLayer();
    private void RenameLayer_Click(object? sender, RoutedEventArgs e) => _ = RenameLayerAsync(Session.Layer);
    private void LayerUp_Click(object? sender, RoutedEventArgs e) => MoveLayer(+1);
    private void LayerDown_Click(object? sender, RoutedEventArgs e) => MoveLayer(-1);
    private void GroupLayer_Click(object? sender, RoutedEventArgs e) => GroupLayer();
    private void MoveOutOfGroup_Click(object? sender, RoutedEventArgs e) => MoveOutOfGroup(Session.Layer);
    private void ToggleVisible_Click(object? sender, RoutedEventArgs e) => ToggleVisible(Session.Layer);
    private void ToggleLock_Click(object? sender, RoutedEventArgs e) => ToggleLock(Session.Layer);

    /// <summary>A layer name not used yet: "Layer 2", "Layer 3"… (in the person's language).</summary>
    private string FreeName(string englishBase)
    {
        var used = Session.Doc.Layers.Select(l => l.Name).ToHashSet();
        var b = L.T(englishBase);
        for (int i = Session.Doc.Layers.Count + 1; ; i++)
            if (!used.Contains(b + " " + i)) return b + " " + i;
    }

    private void ChangeLayers(string editName, Action change)
    {
        Session.DropFloating();
        Session.Apply(LayerStructureEdit.Perform(Session.Doc, editName, change));
    }

    /// <summary>A new empty layer above the current one (Shift+N).</summary>
    internal void NewLayer()
    {
        var layer = new Layer { Name = FreeName("Layer") };
        ChangeLayers("New layer", () => LayerTree.Insert(Session.Doc, layer, Session.Layer));
        Session.Layer = layer;
    }

    internal void NewGroup()
    {
        var group = new Layer { Name = FreeName("Group"), IsGroup = true };
        ChangeLayers("New group", () => LayerTree.Insert(Session.Doc, group, Session.Layer));
        Session.Layer = group;
    }

    internal void DuplicateLayer()
    {
        Layer copy = null!;
        var layer = Session.Layer;
        ChangeLayers("Duplicate layer", () => copy = LayerTree.Duplicate(Session.Doc, layer, " " + L.T("copy")));
        Session.Layer = copy;
    }

    internal void RemoveLayer()
    {
        var layer = Session.Layer;
        var block = LayerTree.Block(Session.Doc, layer);
        if (!Session.Doc.Layers.Except(block).Any(l => !l.IsGroup)) { ShowStatus(L.T("A sprite needs at least one layer")); return; }
        ChangeLayers("Remove layer", () => LayerTree.Remove(Session.Doc, layer));
    }

    internal void MoveLayer(int direction)
    {
        var layer = Session.Layer;
        var sib = LayerTree.Siblings(Session.Doc, layer);
        int j = sib.IndexOf(layer) + Math.Sign(direction);
        if (j < 0 || j >= sib.Count) return;
        ChangeLayers("Move layer", () => LayerTree.Move(Session.Doc, layer, direction));
    }

    /// <summary>Puts the current layer in a new group.</summary>
    internal void GroupLayer()
    {
        var layer = Session.Layer;
        var name = FreeName("Group");
        ChangeLayers("Group layer", () => LayerTree.Group(Session.Doc, layer, name));
    }

    internal void PutIntoGroup(Layer layer, Layer group)
    {
        if (!group.IsGroup || group == layer || LayerTree.IsInside(group, layer)) return;
        ChangeLayers("Put into group", () => LayerTree.PutInto(Session.Doc, layer, group));
    }

    internal void MoveOutOfGroup(Layer layer)
    {
        if (layer.Parent == null) return;
        ChangeLayers("Move out of group", () => LayerTree.MoveOut(Session.Doc, layer));
    }

    internal async Task RenameLayerAsync(Layer layer)
    {
        if (await Dialogs.AskText(L.T("Rename layer"), L.T("Name"), layer.Name) is not { Length: > 0 } name || name == layer.Name) return;
        SetLayerProps(layer, LayerProps.Of(layer) with { Name = name }, "Rename layer");
    }

    internal void ToggleVisible(Layer layer) =>
        SetLayerProps(layer, LayerProps.Of(layer) with { Visible = !layer.Visible }, layer.Visible ? "Hide layer" : "Show layer");

    internal void ToggleLock(Layer layer) =>
        SetLayerProps(layer, LayerProps.Of(layer) with { Locked = !layer.Locked }, layer.Locked ? "Unlock layer" : "Lock layer");

    internal void SetLayerOpacity(Layer layer, int opacity)
    {
        var o = (byte)Math.Clamp(opacity, 0, 255);
        if (o != layer.Opacity) SetLayerProps(layer, LayerProps.Of(layer) with { Opacity = o }, "Layer opacity");
    }

    internal void SetLayerBlend(Layer layer, BlendMode blend)
    {
        if (blend != layer.Blend) SetLayerProps(layer, LayerProps.Of(layer) with { Blend = blend }, "Blend mode");
    }

    private void SetLayerProps(Layer layer, LayerProps props, string editName)
    {
        Session.DropFloating();
        Session.Apply(LayerPropsEdit.Perform(layer, props, editName));
    }
}
