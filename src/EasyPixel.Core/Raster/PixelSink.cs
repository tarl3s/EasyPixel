using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

/// <summary>
/// Where every drawing operation writes. Ignores pixels outside the image or rejected by the clip (a selection),
/// and records the rectangle that was touched so the caller can build an undo step from it.
/// </summary>
public sealed class PixelSink
{
    private readonly Func<int, int, bool>? _clip;
    private int _minX = int.MaxValue, _minY = int.MaxValue, _maxX = int.MinValue, _maxY = int.MinValue;

    public PixelSink(Image img, Func<int, int, bool>? clip = null)
    {
        Image = img;
        _clip = clip;
    }

    public Image Image { get; }

    /// <summary>Bounding box of every pixel written so far (empty when nothing was).</summary>
    public PixelRect Dirty => _maxX < _minX ? default : new PixelRect(_minX, _minY, _maxX - _minX + 1, _maxY - _minY + 1);

    /// <summary>True when (x, y) is inside the image and allowed by the clip.</summary>
    public bool CanWrite(int x, int y) =>
        (uint)x < (uint)Image.Width && (uint)y < (uint)Image.Height && (_clip == null || _clip(x, y));

    public void Set(int x, int y, uint v)
    {
        if (!CanWrite(x, y)) return;
        Image.Pixels[y * Image.Width + x] = v;
        if (x < _minX) _minX = x;
        if (x > _maxX) _maxX = x;
        if (y < _minY) _minY = y;
        if (y > _maxY) _maxY = y;
    }

    public uint Get(int x, int y) => Image.Get(x, y);

    /// <summary>True when a clip restricts writes (callers can then skip per-pixel checks).</summary>
    public bool HasClip => _clip != null;

    /// <summary>Sets the pixels x0..x1 of row y (inclusive). Without a clip this is one pass; with one it goes pixel by pixel.</summary>
    public void SetSpan(int y, int x0, int x1, uint v)
    {
        if (_clip != null) { for (int x = x0; x <= x1; x++) Set(x, y, v); return; }
        x0 = Math.Max(x0, 0);
        x1 = Math.Min(x1, Image.Width - 1);
        if ((uint)y >= (uint)Image.Height || x1 < x0) return;
        Array.Fill(Image.Pixels, v, y * Image.Width + x0, x1 - x0 + 1);
        if (x0 < _minX) _minX = x0;
        if (x1 > _maxX) _maxX = x1;
        if (y < _minY) _minY = y;
        if (y > _maxY) _maxY = y;
    }
}
