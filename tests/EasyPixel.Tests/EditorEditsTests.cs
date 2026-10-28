using EasyPixel.Core.Formats;
using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Tests;

/// <summary>The structural edits the editor's panels and menus use: layers, frames, tags, palette, whole-sprite changes.</summary>
public class EditorEditsTests
{
    private static Layer AddLayer(Sprite s, string name, Layer? above = null)
    {
        var l = new Layer { Name = name };
        LayerTree.Insert(s, l, above ?? s.Layers[^1]);
        return l;
    }

    private static string Names(Sprite s) => string.Join(",", s.Layers.Select(l => l.Name + (l.Parent != null ? "<" + l.Parent.Name : "")));

    // ---- layers ----

    [Fact]
    public void AddLayer_UndoRedo_KeepsCelsDrawnLater()
    {
        var s = new Sprite(4, 4);
        var h = new History(s);
        Layer added = null!;
        h.Push(LayerStructureEdit.Perform(s, "Add layer", () => added = AddLayer(s, "B")));
        s.GetOrCreateCel(added, 0).Image.Set(1, 1, 7);
        h.Undo();
        Assert.Single(s.Layers);
        Assert.DoesNotContain(s.Cels.Keys, k => k.Item1 == added);
        h.Redo();
        Assert.Equal("Layer,B", Names(s));
        Assert.Equal(7u, s.Cels[(added, 0)].Image.Get(1, 1));
    }

    [Fact]
    public void RemoveGroup_RemovesMembersAndCels_UndoBringsThemBack()
    {
        var s = new Sprite(4, 4);
        var a = AddLayer(s, "A");
        var g = LayerTree.Group(s, a, "G");
        s.GetOrCreateCel(a, 0).Image.Set(0, 0, 5);
        var h = new History(s);
        h.Push(LayerStructureEdit.Perform(s, "Remove layer", () => Assert.True(LayerTree.Remove(s, g))));
        Assert.Equal("Layer", Names(s));
        Assert.Empty(s.Cels.Keys.Where(k => k.Item1 == a));
        h.Undo();
        Assert.Equal("Layer,G,A<G", Names(s));
        Assert.Equal(5u, s.Cels[(a, 0)].Image.Get(0, 0));
        h.Redo();
        Assert.Equal("Layer", Names(s));
    }

    [Fact]
    public void Remove_LastDrawableLayer_IsRefused()
    {
        var s = new Sprite(4, 4);
        Assert.False(LayerTree.Remove(s, s.Layers[0]));
        Assert.Single(s.Layers);
    }

    [Fact]
    public void Duplicate_CopiesCelsAndSettings_ButNotIdentityKeys()
    {
        var s = new Sprite(4, 4);
        var l = s.Layers[0];
        l.Opacity = 100;
        l.Blend = BlendMode.Multiply;
        l.Extra["ase:id"] = new byte[] { 1, 0, 0, 0 };
        l.Extra["ase:uuid"] = new byte[16];
        l.Extra["ase:after"] = new byte[] { 9 };
        var cel = s.GetOrCreateCel(l, 0);
        cel.Image.Set(2, 2, 3);
        var h = new History(s);
        Layer copy = null!;
        h.Push(LayerStructureEdit.Perform(s, "Duplicate layer", () => copy = LayerTree.Duplicate(s, l, " copy")));
        Assert.Equal("Layer,Layer copy", Names(s));
        Assert.False(copy.Extra.ContainsKey("ase:id"));
        Assert.False(copy.Extra.ContainsKey("ase:uuid"));
        Assert.Equal(new byte[] { 9 }, copy.Extra["ase:after"]);
        Assert.Equal((byte)100, copy.Opacity);
        Assert.Equal(BlendMode.Multiply, copy.Blend);
        var copyCel = s.Cels[(copy, 0)];
        Assert.NotSame(cel, copyCel);
        Assert.NotSame(cel.Image, copyCel.Image);
        Assert.Equal(3u, copyCel.Image.Get(2, 2));
        h.Undo();
        Assert.Equal("Layer", Names(s));
        Assert.DoesNotContain(s.Cels.Keys, k => k.Item1 == copy);
    }

    [Fact]
    public void Duplicate_Group_CopiesMembersAndKeepsLinkedCelsLinked()
    {
        var s = new Sprite(4, 4);
        s.Frames.Add(new Frame());
        var a = AddLayer(s, "A");
        var g = LayerTree.Group(s, a, "G");
        var linked = new Cel(new Image(4, 4));
        s.Cels[(a, 0)] = linked;
        s.Cels[(a, 1)] = linked;
        var copy = LayerTree.Duplicate(s, g, " copy");
        Assert.Equal("Layer,G,A<G,G copy,A<G copy", Names(s));
        var a2 = s.Layers[4];
        Assert.Same(s.Cels[(a2, 0)], s.Cels[(a2, 1)]);
        Assert.NotSame(linked, s.Cels[(a2, 0)]);
        Assert.Same(copy, a2.Parent);
    }

    [Fact]
    public void Move_UpAndDown_MovesWholeGroups_AndUndoes()
    {
        var s = new Sprite(4, 4);
        var b = AddLayer(s, "B");
        var g = LayerTree.Group(s, b, "G");
        var c = AddLayer(s, "C", g); // above the group, at the top level
        Assert.Equal("Layer,G,B<G,C", Names(s));
        var h = new History(s);
        h.Push(LayerStructureEdit.Perform(s, "Move layer", () => Assert.True(LayerTree.Move(s, g, +1))));
        Assert.Equal("Layer,C,G,B<G", Names(s));
        Assert.False(LayerTree.Move(s, g, +1));
        Assert.True(LayerTree.Move(s, g, -1));
        Assert.True(LayerTree.Move(s, g, -1));
        Assert.Equal("G,B<G,Layer,C", Names(s));
        Assert.False(LayerTree.Move(s, b, -1)); // alone in its group
        h.Undo();
        Assert.Equal("Layer,G,B<G,C", Names(s));
        Assert.Same(g, b.Parent);
        Assert.Null(c.Parent);
    }

    [Fact]
    public void PutInto_AndMoveOut_ChangeGroups_AndUndo()
    {
        var s = new Sprite(4, 4);
        var a = AddLayer(s, "A");
        var g = LayerTree.Group(s, a, "G");
        var bottom = s.Layers[0];
        var h = new History(s);
        h.Push(LayerStructureEdit.Perform(s, "Put into group", () => Assert.True(LayerTree.PutInto(s, bottom, g))));
        Assert.Equal("G,A<G,Layer<G", Names(s));
        Assert.False(LayerTree.PutInto(s, g, g));
        h.Push(LayerStructureEdit.Perform(s, "Move out of group", () => Assert.True(LayerTree.MoveOut(s, a))));
        Assert.Equal("G,Layer<G,A", Names(s));
        h.Undo();
        h.Undo();
        Assert.Equal("Layer,G,A<G", Names(s));
        Assert.Null(bottom.Parent);
        Assert.Same(g, a.Parent);
    }

    [Fact]
    public void LayerProps_UndoRedo()
    {
        var s = new Sprite(4, 4);
        var l = s.Layers[0];
        var h = new History(s);
        h.Push(LayerPropsEdit.Perform(l, LayerProps.Of(l) with { Name = "Ink", Visible = false, Locked = true, Opacity = 10, Blend = BlendMode.Screen }, "Layer properties"));
        Assert.Equal(("Ink", false, true, (byte)10, BlendMode.Screen), (l.Name, l.Visible, l.Locked, l.Opacity, l.Blend));
        h.Undo();
        Assert.Equal(("Layer", true, false, (byte)255, BlendMode.Normal), (l.Name, l.Visible, l.Locked, l.Opacity, l.Blend));
        h.Redo();
        Assert.Equal("Ink", l.Name);
    }

    // ---- frames and tags ----

    private static Sprite Frames3(out Layer l)
    {
        var s = new Sprite(2, 2);
        s.Frames.Add(new Frame { DurationMs = 200 });
        s.Frames.Add(new Frame { DurationMs = 300 });
        l = s.Layers[0];
        for (int f = 0; f < 3; f++) s.GetOrCreateCel(l, f).Image.Set(0, 0, (uint)(f + 1));
        return s;
    }

    [Fact]
    public void MoveFrame_MovesCelsAndDuration_AndUndoes()
    {
        var s = Frames3(out var l);
        var h = new History(s);
        h.Push(MoveFrameEdit.Perform(s, 0, 2));
        Assert.Equal(new[] { 200, 300, 100 }, s.Frames.Select(f => f.DurationMs));
        Assert.Equal(new uint[] { 2, 3, 1 }, Enumerable.Range(0, 3).Select(f => s.Cels[(l, f)].Image.Get(0, 0)));
        h.Undo();
        Assert.Equal(new uint[] { 1, 2, 3 }, Enumerable.Range(0, 3).Select(f => s.Cels[(l, f)].Image.Get(0, 0)));
        h.Redo();
        h.Push(MoveFrameEdit.Perform(s, 2, 0));
        Assert.Equal(new uint[] { 1, 2, 3 }, Enumerable.Range(0, 3).Select(f => s.Cels[(l, f)].Image.Get(0, 0)));
    }

    [Fact]
    public void FrameDuration_UndoRedo()
    {
        var s = Frames3(out _);
        var h = new History(s);
        h.Push(FrameDurationEdit.Perform(s, new[] { 0, 2 }, 50));
        Assert.Equal(new[] { 50, 200, 50 }, s.Frames.Select(f => f.DurationMs));
        h.Undo();
        Assert.Equal(new[] { 100, 200, 300 }, s.Frames.Select(f => f.DurationMs));
        h.Redo();
        Assert.Equal(50, s.Frames[2].DurationMs);
    }

    [Fact]
    public void DuplicateFrame_CopiesCels_UndoRemovesThem()
    {
        var s = Frames3(out var l);
        var h = new History(s);
        h.Push(FrameOps.Duplicate(s, 1));
        Assert.Equal(4, s.Frames.Count);
        Assert.Equal(200, s.Frames[2].DurationMs);
        Assert.Equal(2u, s.Cels[(l, 2)].Image.Get(0, 0));
        Assert.NotSame(s.Cels[(l, 1)], s.Cels[(l, 2)]);
        Assert.Equal(3u, s.Cels[(l, 3)].Image.Get(0, 0));
        h.Undo();
        Assert.Equal(3, s.Frames.Count);
        Assert.Equal(3u, s.Cels[(l, 2)].Image.Get(0, 0));
        h.Redo();
        Assert.Equal(2u, s.Cels[(l, 2)].Image.Get(0, 0));
    }

    [Fact]
    public void Tags_CreateEditRemove_Undo()
    {
        var s = Frames3(out _);
        var h = new History(s);
        var before = TagsEdit.Take(s);
        var tag = new Tag { Name = "walk", From = 0, To = 1 };
        s.Tags.Add(tag);
        h.Push(TagsEdit.Capture(s, before, "New tag"));
        before = TagsEdit.Take(s);
        tag.Name = "run"; tag.Dir = LoopDir.PingPong; tag.Color = new Rgba(1, 2, 3, 255);
        h.Push(TagsEdit.Capture(s, before, "Tag properties"));
        before = TagsEdit.Take(s);
        s.Tags.Clear();
        h.Push(TagsEdit.Capture(s, before, "Remove tag"));
        h.Undo();
        Assert.Equal(("run", LoopDir.PingPong), (s.Tags.Single().Name, s.Tags[0].Dir));
        h.Undo();
        Assert.Equal(("walk", LoopDir.Forward, 0, 1), (tag.Name, tag.Dir, tag.From, tag.To));
        h.Undo();
        Assert.Empty(s.Tags);
    }

    // ---- palette ----

    [Fact]
    public void MovePaletteEntry_Indexed_RenumbersPixels_AndUndoes()
    {
        var s = new Sprite(2, 1, ColorMode.Indexed);
        s.Palette.Colors.Add(new Rgba(255, 0, 0, 255)); // 1
        s.Palette.Colors.Add(new Rgba(0, 255, 0, 255)); // 2
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        cel.Image.Set(0, 0, 1);
        cel.Image.Set(1, 0, 2);
        var h = new History(s);
        h.Push(SpriteStateEdit.Perform(s, "Move colour", () => s.MovePaletteEntry(2, 0)));
        Assert.Equal(new Rgba(0, 255, 0, 255), s.Palette.Colors[0]);
        Assert.Equal(1, s.Palette.TransparentIndex);
        Assert.Equal((2u, 0u), (cel.Image.Get(0, 0), cel.Image.Get(1, 0)));
        h.Undo();
        Assert.Equal((1u, 2u), (cel.Image.Get(0, 0), cel.Image.Get(1, 0)));
        Assert.Equal(0, s.Palette.TransparentIndex);
        Assert.Equal(new Rgba(0, 255, 0, 255), s.Palette.Colors[2]);
    }

    [Fact]
    public void PaletteEdit_AddAndRemove_Undo()
    {
        var s = new Sprite(2, 2);
        var h = new History(s);
        h.Push(PaletteEdit.Perform(s, "Add colour", () => s.Palette.Add(new Rgba(1, 1, 1, 255))));
        h.Push(PaletteEdit.Perform(s, "Add colour", () => s.Palette.Add(new Rgba(2, 2, 2, 255))));
        h.Push(PaletteEdit.Perform(s, "Remove colour", () => s.Palette.Colors.RemoveAt(0)));
        Assert.Equal(new[] { new Rgba(2, 2, 2, 255) }, s.Palette.Colors);
        h.Undo();
        Assert.Equal(2, s.Palette.Colors.Count);
        h.Undo();
        h.Undo();
        Assert.Empty(s.Palette.Colors);
    }

    [Theory]
    [InlineData(".gpl")]
    [InlineData(".hex")]
    [InlineData(".pal")]
    public void PaletteFile_RoundTrip(string ext)
    {
        var colors = new List<Rgba> { new(0, 0, 0, 255), new(255, 128, 7, 255), new(18, 52, 86, 255) };
        var ms = new MemoryStream();
        PaletteFormats.Write(ms, ext, colors, "Test");
        ms.Position = 0;
        Assert.Equal(colors, PaletteFormats.Read(ms, ext));
    }

    [Fact]
    public void PaletteFile_ReadsForeignFiles_AndRejectsBrokenOnes()
    {
        static List<Rgba> Read(string text, string ext) => PaletteFormats.Read(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)), ext);
        var gpl = Read("GIMP Palette\r\nName: Lospec\r\nColumns: 4\r\n#\r\n  0  0  0\tBlack\r\n255 255 255 White\r\n", ".gpl");
        Assert.Equal(new[] { new Rgba(0, 0, 0, 255), new Rgba(255, 255, 255, 255) }, gpl);
        Assert.Equal(new[] { new Rgba(0xAB, 0xCD, 0xEF, 255) }, Read("#abcdef\n\n", ".hex"));
        Assert.Equal(new[] { new Rgba(1, 2, 3, 255) }, Read("JASC-PAL\n0100\n1\n1 2 3\n", ".pal"));
        Assert.Throws<FormatException>(() => Read("not a palette", ".gpl"));
        Assert.Throws<FormatException>(() => Read("12345\n", ".hex"));
        Assert.Throws<FormatException>(() => Read("JASC-PAL\n0100\n3\n1 2 3\n", ".pal"));
        Assert.Throws<FormatException>(() => Read("GIMP Palette\n300 0 0\n", ".gpl"));
        Assert.Throws<FormatException>(() => Read("", ".hex"));
    }

    // ---- whole sprite ----

    private static Sprite Marked(int w, int h, out Cel cel)
    {
        var s = new Sprite(w, h);
        cel = s.GetOrCreateCel(s.Layers[0], 0);
        cel.Image.Set(0, 0, 0xFF0000FF); // top-left red
        return s;
    }

    [Fact]
    public void Rotate90_SwapsSize_MovesPixels_AndUndoes()
    {
        var s = Marked(4, 2, out var cel);
        var h = new History(s);
        h.Push(SpriteStateEdit.Perform(s, "Rotate", () => SpriteTransforms.Rotate(s, SpriteRotation.Clockwise90)));
        Assert.Equal((2, 4), (s.Width, s.Height));
        Assert.Equal(0xFF0000FFu, cel.Image.Get(1 - cel.X, 0 - cel.Y)); // top-left -> top-right
        h.Undo();
        Assert.Equal((4, 2), (s.Width, s.Height));
        Assert.Equal(0xFF0000FFu, cel.Image.Get(0, 0));
        SpriteTransforms.Rotate(s, SpriteRotation.CounterClockwise90);
        Assert.Equal(0xFF0000FFu, cel.Image.Get(0 - cel.X, 3 - cel.Y)); // top-left -> bottom-left
        SpriteTransforms.Rotate(s, SpriteRotation.Clockwise90);
        SpriteTransforms.Rotate(s, SpriteRotation.Half);
        Assert.Equal(0xFF0000FFu, cel.Image.Get(3 - cel.X, 1 - cel.Y));
    }

    [Fact]
    public void Flip_MovesTrimmedCel()
    {
        var s = new Sprite(10, 4);
        var cel = new Cel(new Image(2, 1, 0xFF00FF00), 1, 0); // covers x 1..2
        s.Cels[(s.Layers[0], 0)] = cel;
        SpriteTransforms.Flip(s, horizontal: true);
        Assert.Equal((7, 0), (cel.X, cel.Y)); // now covers x 7..8
        SpriteTransforms.Flip(s, horizontal: false);
        Assert.Equal((7, 3), (cel.X, cel.Y));
    }

    [Fact]
    public void Scale_ResizesCanvasAndCels_AndUndoes()
    {
        var s = Marked(4, 4, out var cel);
        var h = new History(s);
        h.Push(SpriteStateEdit.Perform(s, "Resize", () => SpriteTransforms.Scale(s, 8, 12)));
        Assert.Equal((8, 12), (s.Width, s.Height));
        Assert.Equal((8, 12), (cel.Image.Width, cel.Image.Height));
        Assert.Equal(0xFF0000FFu, cel.Image.Get(1, 2));
        Assert.Equal(0u, cel.Image.Get(2, 3));
        h.Undo();
        Assert.Equal((4, 4, 4), (s.Width, s.Height, cel.Image.Width));
    }

    [Fact]
    public void ColorMode_And_ReduceColors_Undo()
    {
        var s = new Sprite(4, 1);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        uint[] px = { 0xFF000000, 0xFF101010, 0xFFF0F0F0, 0xFFFFFFFF };
        px.CopyTo(cel.Image.Pixels, 0);
        var h = new History(s);
        h.Push(SpriteStateEdit.Perform(s, "Reduce colours", () => SpriteTransforms.ReduceColors(s, 2)));
        Assert.Equal(2, cel.Image.Pixels.Distinct().Count());
        Assert.Equal(2, s.Palette.Colors.Count);
        h.Push(SpriteStateEdit.Perform(s, "Colour mode", () => s.ConvertMode(ColorMode.Indexed)));
        Assert.Equal(ColorMode.Indexed, s.Mode);
        h.Undo();
        h.Undo();
        Assert.Equal(ColorMode.Rgba, s.Mode);
        Assert.Equal(px, cel.Image.Pixels);
        Assert.Empty(s.Palette.Colors);
    }

    [Fact]
    public void ReduceColors_Indexed_StaysIndexed()
    {
        var s = new Sprite(3, 1, ColorMode.Indexed);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        for (int x = 0; x < 3; x++) cel.Image.Set(x, 0, s.ColorToPixel(new Rgba((byte)(x * 100), 0, 0, 255)));
        SpriteTransforms.ReduceColors(s, 2);
        Assert.Equal(ColorMode.Indexed, s.Mode);
        Assert.True(s.Palette.Colors.Count <= 3); // 2 colours + transparent
        Assert.Equal(0, s.Palette.TransparentIndex);
    }

    [Fact]
    public void CanvasSize_MovesLinkedCelOnce()
    {
        var s = new Sprite(4, 4);
        s.Frames.Add(new Frame());
        var cel = new Cel(new Image(4, 4));
        s.Cels[(s.Layers[0], 0)] = cel;
        s.Cels[(s.Layers[0], 1)] = cel;
        var h = new History(s);
        h.Push(ResizeEdit.Perform(s, 8, 8, Anchor.BottomRight));
        Assert.Equal((4, 4), (cel.X, cel.Y));
        h.Undo();
        Assert.Equal((0, 0), (cel.X, cel.Y));
    }

    // ---- review fixes ----

    [Fact]
    public void MoveFrame_TagsFollowTheirFrames_AndUndo()
    {
        var s = Frames3(out _);
        s.Frames.Add(new Frame());
        var tag = new Tag { Name = "t", From = 1, To = 2 };
        s.Tags.Add(tag);
        var h = new History(s);
        h.Push(MoveFrameEdit.Perform(s, 2, 0)); // the tag's last frame moves before its first
        Assert.Equal((0, 2), (tag.From, tag.To));
        h.Undo();
        Assert.Equal((1, 2), (tag.From, tag.To));
        h.Redo();
        Assert.Equal((0, 2), (tag.From, tag.To));
        h.Undo();
        h.Push(MoveFrameEdit.Perform(s, 3, 0)); // a frame outside moves in front: the tag shifts with its frames
        Assert.Equal((2, 3), (tag.From, tag.To));
    }

    [Fact]
    public void OpacityEdits_MergeUntilAnotherStep()
    {
        var s = new Sprite(2, 2);
        var l = s.Layers[0];
        var h = new History(s);
        void Opacity(byte o) => h.Push(LayerPropsEdit.Perform(l, LayerProps.Of(l) with { Opacity = o }, "Layer opacity"));
        Opacity(200); Opacity(150); Opacity(100);
        Assert.Equal(1, h.Count);
        h.Push(LayerPropsEdit.Perform(l, LayerProps.Of(l) with { Visible = false }, "Hide layer"));
        Opacity(50);
        Assert.Equal(3, h.Count);
        h.Undo();
        Opacity(60); // after an undo a new step starts
        Assert.Equal(3, h.Count);
        h.MarkSaved();
        Opacity(70); // the saved state stays reachable
        Assert.Equal(4, h.Count);
        h.Undo();
        Assert.Equal((byte)60, l.Opacity);
        h.Undo(); h.Undo();
        Assert.Equal((byte)100, l.Opacity);
        h.Undo();
        Assert.Equal((byte)255, l.Opacity);
    }

    [Fact]
    public void SelectByValue_UsesTheFillRule()
    {
        var img = new Image(3, 1);
        img.Pixels[0] = 0xFF000010; img.Pixels[1] = 0xFF000014; img.Pixels[2] = 0xFF000040;
        var exact = Core.Select.Selectors.ByValue(img, 0xFF000010, 0);
        Assert.Equal((true, false, false), (exact.Contains(0, 0), exact.Contains(1, 0), exact.Contains(2, 0)));
        var near = Core.Select.Selectors.ByValue(img, 0xFF000010, 8);
        Assert.Equal((true, true, false), (near.Contains(0, 0), near.Contains(1, 0), near.Contains(2, 0)));
    }

    [Fact]
    public void Aseprite_CollapsedGroup_IsReported()
    {
        var s = new Sprite(2, 2);
        var g = LayerTree.Group(s, s.Layers[0], "G");
        Assert.False(AsepriteFormat.IsCollapsed(g));
        g.Extra["ase:flags"] = new byte[] { 1 | 2 | 32, 0, 1, 0, 0, 0, 0, 0 };
        var ms = new MemoryStream();
        new AsepriteFormat().Write(s, ms, new List<string>());
        ms.Position = 0;
        var read = new AsepriteFormat().Read(ms, new List<string>());
        var group = read.Layers.Single(l => l.IsGroup);
        Assert.True(AsepriteFormat.IsCollapsed(group));
        Assert.False(AsepriteFormat.IsCollapsed(read.Layers.Single(l => !l.IsGroup)));
    }
}
