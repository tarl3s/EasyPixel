using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>Pixel change of one cel, stored as before/after copies of the dirty rectangle only.</summary>
public sealed class PixelEdit : IEdit
{
    private const long Overhead = 64;

    private readonly Cel _cel;
    private readonly PixelRect _rect;
    private readonly uint[] _before;
    private readonly uint[] _after;

    private PixelEdit(string name, Cel cel, PixelRect rect, uint[] before, uint[] after)
    {
        Name = name;
        _cel = cel;
        _rect = rect;
        _before = before;
        _after = after;
    }

    public string Name { get; }
    public long Bytes => (_before.Length + _after.Length) * 4L + Overhead;

    /// <summary>Copies the pixels of <paramref name="r"/> (clipped to the image); call it before drawing.</summary>
    public static uint[] Snapshot(Image img, PixelRect r) => Read(img, Clip(img, r));

    /// <summary>
    /// Records a stroke: <paramref name="before"/> is <see cref="Snapshot"/> of <paramref name="dirty"/> taken before
    /// drawing; the "after" pixels are copied from the cel's image now. The rectangle is in image coordinates.
    /// </summary>
    public static PixelEdit Capture(Cel cel, PixelRect dirty, uint[] before, string name)
    {
        var r = Clip(cel.Image, dirty);
        if (before.Length != r.W * r.H)
            throw new ArgumentException("'before' does not match the dirty rectangle clipped to the image.", nameof(before));
        return new PixelEdit(name, cel, r, before, Read(cel.Image, r));
    }

    public void Undo(Sprite s) => Write(_cel.Image, _rect, _before);
    public void Redo(Sprite s) => Write(_cel.Image, _rect, _after);

    private static PixelRect Clip(Image img, PixelRect r) => r.Intersect(new PixelRect(0, 0, img.Width, img.Height));

    private static uint[] Read(Image img, PixelRect r)
    {
        var buf = new uint[r.W * r.H];
        for (int y = 0; y < r.H; y++)
            Array.Copy(img.Pixels, (r.Y + y) * img.Width + r.X, buf, y * r.W, r.W);
        return buf;
    }

    private static void Write(Image img, PixelRect r, uint[] src)
    {
        for (int y = 0; y < r.H; y++)
            Array.Copy(src, y * r.W, img.Pixels, (r.Y + y) * img.Width + r.X, r.W);
    }
}
