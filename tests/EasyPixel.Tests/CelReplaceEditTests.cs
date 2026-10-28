using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel.Tests;

public class CelReplaceEditTests
{
    [Fact]
    public void DropThatGrowsCel_UndoRestoresImageAndPosition()
    {
        var s = new Sprite(4, 4);
        var h = new History(s);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        cel.Image.Set(1, 1, 0xFF0000FF);
        var oldImage = cel.Image;

        var before = CelReplaceEdit.Begin(cel);
        var sel = Selectors.Rect(4, 4, new PixelRect(1, 1, 1, 1));
        var f = Floating.Lift(cel, sel);
        f.X = -2; f.Y = -3; // dropped outside the cel: it grows up-left
        f.Drop(cel);
        h.Push(CelReplaceEdit.Capture(cel, before, "Move"));
        Assert.Equal((-2, -3), (cel.X, cel.Y));

        Assert.True(h.Undo());
        Assert.Equal((0, 0), (cel.X, cel.Y));
        Assert.Equal((4, 4), (cel.Image.Width, cel.Image.Height));
        Assert.Equal(0xFF0000FFu, cel.Image.Get(1, 1));
        Assert.NotSame(oldImage, cel.Image); // the edit keeps its own copy of the old pixels

        Assert.True(h.Redo());
        Assert.Equal((-2, -3), (cel.X, cel.Y));
        Assert.Equal(0u, cel.Image.Get(3, 4)); // old spot (1,1) in grown-cel coordinates is cleared
        Assert.Equal(0xFF0000FFu, cel.Image.Get(0, 0));
    }

    [Fact]
    public void Bytes_CountsBothImages()
    {
        var s = new Sprite(10, 10);
        var cel = s.GetOrCreateCel(s.Layers[0], 0);
        var before = CelReplaceEdit.Begin(cel);
        cel.Image = new Image(20, 20);
        var e = CelReplaceEdit.Capture(cel, before, "Resize");
        Assert.True(e.Bytes >= (100 + 400) * 4);
        Assert.Equal("Resize", e.Name);
    }
}
