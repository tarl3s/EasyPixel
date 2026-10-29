using EasyPixel.Core.History;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public class HistoryTests
{
    private static (Sprite s, History h, Cel cel) Setup(int w = 8, int h = 8)
    {
        var s = new Sprite(w, h);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        return (s, new History(s), cel);
    }

    private static PixelEdit Paint(Cel cel, int x, int y, uint v, string name = "Pencil")
    {
        var r = new PixelRect(x, y, 1, 1);
        var before = PixelEdit.Snapshot(cel.Image, r);
        cel.Image.Set(x, y, v);
        return PixelEdit.Capture(cel, r, before, name);
    }

    [Fact]
    public void Undo_RestoresPixels()
    {
        var (_, h, cel) = Setup();
        h.Push(Paint(cel, 2, 3, 0xFF0000FF));
        Assert.True(h.Undo());
        Assert.Equal(0u, cel.Image.Get(2, 3));
        Assert.False(h.Undo());
    }

    [Fact]
    public void Redo_AfterUndo()
    {
        var (_, h, cel) = Setup();
        h.Push(Paint(cel, 2, 3, 0xFF0000FF));
        h.Undo();
        Assert.True(h.Redo());
        Assert.Equal(0xFF0000FFu, cel.Image.Get(2, 3));
        Assert.False(h.Redo());
    }

    [Fact]
    public void NewEditClearsRedo()
    {
        var (_, h, cel) = Setup();
        h.Push(Paint(cel, 1, 1, 1));
        h.Undo();
        h.Push(Paint(cel, 2, 2, 2));
        Assert.False(h.Redo());
        Assert.Equal(1, h.Count);
    }

    [Fact]
    public void IsDirty_FalseAfterMarkSaved_TrueAfterEdit()
    {
        var (_, h, cel) = Setup();
        Assert.False(h.IsDirty);
        h.Push(Paint(cel, 1, 1, 1));
        Assert.True(h.IsDirty);
        h.MarkSaved();
        Assert.False(h.IsDirty);
        h.Push(Paint(cel, 2, 2, 2));
        Assert.True(h.IsDirty);
        h.Undo();
        Assert.False(h.IsDirty);
        h.Undo();
        Assert.True(h.IsDirty);
    }

    [Fact]
    public void IsDirty_StaysTrue_WhenSavedStateWasInDiscardedRedo()
    {
        var (_, h, cel) = Setup();
        h.Push(Paint(cel, 1, 1, 1));
        h.MarkSaved();
        h.Undo();
        h.Push(Paint(cel, 2, 2, 2));
        h.Undo();
        h.Redo();
        Assert.True(h.IsDirty);
    }

    [Fact]
    public void BigCanvas_OnePixelEdit_StoresSmallDiff()
    {
        var (_, h, cel) = Setup(4096, 4096);
        var e = Paint(cel, 100, 100, 0xFFFFFFFF);
        Assert.True(e.Bytes < 1024);
        h.Push(e);
        h.Undo();
        Assert.Equal(0u, cel.Image.Get(100, 100));
    }

    [Fact]
    public void Capture_ClipsDirtyRectToImage()
    {
        var (_, h, cel) = Setup(4, 4);
        var r = new PixelRect(-2, -2, 4, 4);
        var before = PixelEdit.Snapshot(cel.Image, r);
        Assert.Equal(4, before.Length);
        cel.Image.Set(0, 0, 9);
        h.Push(PixelEdit.Capture(cel, r, before, "x"));
        h.Undo();
        Assert.Equal(0u, cel.Image.Get(0, 0));
    }

    [Fact]
    public void Cap_DropsOldest()
    {
        var (_, h, cel) = Setup();
        for (int i = 0; i < 10; i++) h.Push(Paint(cel, i % 8, 0, (uint)i + 1));
        long one = Paint(cel, 0, 1, 1).Bytes;
        h.MaxBytes = one * 3;
        Assert.Equal(3, h.Count);
        Assert.True(h.TotalBytes <= one * 3);
        Assert.True(h.Undo()); Assert.True(h.Undo()); Assert.True(h.Undo());
        Assert.False(h.Undo());
    }

    [Fact]
    public void Cap_DroppingSavedPosition_KeepsDirty()
    {
        var (_, h, cel) = Setup();
        h.Push(Paint(cel, 0, 0, 1));
        h.MarkSaved();
        h.MaxBytes = Paint(cel, 7, 7, 1).Bytes * 2;
        cel.Image.Set(7, 7, 0);
        h.Push(Paint(cel, 1, 0, 2));
        h.Push(Paint(cel, 2, 0, 3));
        h.Undo(); h.Undo();
        Assert.False(h.IsDirty); // saved state is now the oldest reachable one
        h.Redo(); h.Redo();
        h.Push(Paint(cel, 3, 0, 4)); // drops the edit that led to the saved state
        while (h.Undo()) { }
        Assert.True(h.IsDirty);
    }

    [Fact]
    public void AddRemoveFrame_RestoresCelsAndShifts()
    {
        var (s, h, cel0) = Setup();
        var layer = s.Layers[0];
        s.Frames.Add(new Frame());
        var cel1 = s.GetOrCreateCel(layer, 1);
        var f = new Frame { DurationMs = 250 };
        h.Push(AddFrameEdit.Perform(s, f, 1));
        Assert.Equal(3, s.Frames.Count);
        Assert.Same(cel1, s.Cels[(layer, 2)]);
        Assert.False(s.Cels.ContainsKey((layer, 1)));

        h.Push(RemoveFrameEdit.Perform(s, 0));
        Assert.Equal(2, s.Frames.Count);
        Assert.Same(cel1, s.Cels[(layer, 1)]);
        Assert.False(s.Cels.ContainsKey((layer, 2)));

        h.Undo();
        Assert.Equal(3, s.Frames.Count);
        Assert.Same(cel0, s.Cels[(layer, 0)]);
        Assert.Same(cel1, s.Cels[(layer, 2)]);
        h.Undo();
        Assert.Equal(2, s.Frames.Count);
        Assert.Same(cel0, s.Cels[(layer, 0)]);
        Assert.Same(cel1, s.Cels[(layer, 1)]);
        Assert.Equal(2, s.Cels.Count);
        h.Redo(); h.Redo();
        Assert.Equal(2, s.Frames.Count);
        Assert.Same(f, s.Frames[0]);
        Assert.Same(cel1, s.Cels[(layer, 1)]);
    }

    [Fact]
    public void Resize_UndoRestoresSizeAndPositions()
    {
        var (s, h, cel) = Setup(8, 8);
        cel.X = 3; cel.Y = 2;
        h.Push(ResizeEdit.Perform(s, 20, 12, Anchor.Center));
        Assert.Equal((20, 12), (s.Width, s.Height));
        Assert.Equal((9, 4), (cel.X, cel.Y));
        h.Undo();
        Assert.Equal((8, 8), (s.Width, s.Height));
        Assert.Equal((3, 2), (cel.X, cel.Y));
        h.Redo();
        Assert.Equal((20, 12), (s.Width, s.Height));
        Assert.Equal((9, 4), (cel.X, cel.Y));
    }

    [Fact]
    public void Capture_RectOutsideImage_IsEmptyEdit()
    {
        var (_, h, cel) = Setup(4, 4);
        var r = new PixelRect(10, 10, 3, 3);
        var before = PixelEdit.Snapshot(cel.Image, r);
        Assert.Empty(before);
        var e = PixelEdit.Capture(cel, r, before, "x");
        h.Push(e);
        Assert.True(h.Undo());
        Assert.True(h.Redo());
    }

    [Fact]
    public void AddFrame_UndoRemovesPaintedCels_RedoRestoresThem()
    {
        var (s, h, cel0) = Setup();
        var layer = s.Layers[0];
        s.Frames.Add(new Frame());
        var cel1 = s.GetOrCreateCel(layer, 1);
        h.Push(AddFrameEdit.Perform(s, new Frame(), 1));
        s.GetOrCreateCel(layer, 1).Image.Set(2, 2, 9);
        h.Undo();
        Assert.Equal(2, s.Cels.Count);
        Assert.Same(cel0, s.Cels[(layer, 0)]);
        Assert.Same(cel1, s.Cels[(layer, 1)]);
        h.Redo();
        Assert.Equal(9u, s.Cels[(layer, 1)].Image.Get(2, 2));
        Assert.Same(cel1, s.Cels[(layer, 2)]);
    }

    private static Tag T(int from, int to) => new() { Name = $"{from}-{to}", From = from, To = to };

    [Fact]
    public void AddFrame_ShiftsAndGrowsTags_UndoRestores()
    {
        var (s, h, _) = Setup();
        for (int i = 0; i < 3; i++) s.Frames.Add(new Frame()); // 4 frames
        s.Tags.AddRange(new[] { T(0, 0), T(1, 2), T(3, 3) });
        h.Push(AddFrameEdit.Perform(s, new Frame(), 2)); // inside 1..2
        Assert.Equal(new[] { (0, 0), (1, 3), (4, 4) }, s.Tags.Select(t => (t.From, t.To)));
        h.Undo();
        Assert.Equal(new[] { (0, 0), (1, 2), (3, 3) }, s.Tags.Select(t => (t.From, t.To)));
        h.Redo();
        Assert.Equal(new[] { (0, 0), (1, 3), (4, 4) }, s.Tags.Select(t => (t.From, t.To)));
    }

    [Fact]
    public void RemoveFrame_ShiftsShrinksAndRemovesEmptyTags_UndoRestores()
    {
        var (s, h, _) = Setup();
        for (int i = 0; i < 4; i++) s.Frames.Add(new Frame()); // 5 frames
        var single = T(2, 2);
        s.Tags.AddRange(new[] { T(0, 1), single, T(1, 3), T(3, 4) });
        h.Push(RemoveFrameEdit.Perform(s, 2));
        Assert.Equal(new[] { (0, 1), (1, 2), (2, 3) }, s.Tags.Select(t => (t.From, t.To)));
        Assert.DoesNotContain(single, s.Tags);
        h.Undo();
        Assert.Equal(new[] { (0, 1), (2, 2), (1, 3), (3, 4) }, s.Tags.Select(t => (t.From, t.To)));
        Assert.Same(single, s.Tags[1]);
        h.Redo();
        Assert.Equal(3, s.Tags.Count);
    }
}
