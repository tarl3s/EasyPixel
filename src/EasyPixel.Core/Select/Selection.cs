using EasyPixel.Core.Model;

namespace EasyPixel.Core.Select;

/// <summary>A set of canvas pixels (a mask over Width x Height, in sprite coordinates). Immutable except through <see cref="Set"/>.</summary>
public sealed class Selection
{
    private readonly bool[] _bits;
    private PixelRect _bounds;
    private bool _boundsDirty;

    public int Width { get; }
    public int Height { get; }

    public Selection(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _bits = new bool[checked(width * height)];
    }

    public static Selection None(int width, int height) => new(width, height);

    public static Selection All(int width, int height)
    {
        var s = new Selection(width, height);
        Array.Fill(s._bits, true);
        s._bounds = new PixelRect(0, 0, width, height);
        return s;
    }

    public bool Contains(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height && _bits[y * Width + x];

    /// <summary>Adds or removes one pixel; writes outside the canvas are ignored.</summary>
    public void Set(int x, int y, bool on)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        int i = y * Width + x;
        if (_bits[i] == on) return;
        _bits[i] = on;
        if (on) { if (!_boundsDirty) _bounds = _bounds.Union(new PixelRect(x, y, 1, 1)); }
        else _boundsDirty = true;
    }

    /// <summary>Smallest rectangle holding every selected pixel (empty when nothing is selected).</summary>
    public PixelRect Bounds
    {
        get
        {
            if (_boundsDirty) { _bounds = Scan(); _boundsDirty = false; }
            return _bounds;
        }
    }

    public bool IsEmpty => Bounds.IsEmpty;

    private PixelRect Scan()
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_bits[y * Width + x])
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    maxY = y;
                }
        return maxX < 0 ? default : new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public Selection Union(Selection o) => Combine(o, (a, b) => a || b);
    public Selection Subtract(Selection o) => Combine(o, (a, b) => a && !b);
    public Selection Intersect(Selection o) => Combine(o, (a, b) => a && b);

    public Selection Invert()
    {
        var r = new Selection(Width, Height);
        for (int i = 0; i < _bits.Length; i++) r._bits[i] = !_bits[i];
        r._boundsDirty = true;
        return r;
    }

    private Selection Combine(Selection o, Func<bool, bool, bool> op)
    {
        if (o.Width != Width || o.Height != Height)
            throw new ArgumentException("Selections must have the same size.", nameof(o));
        var r = new Selection(Width, Height);
        for (int i = 0; i < _bits.Length; i++) r._bits[i] = op(_bits[i], o._bits[i]);
        r._boundsDirty = true;
        return r;
    }

    /// <summary>
    /// A clip for <see cref="Raster.PixelSink"/> working in cel-image coordinates, for a cel placed at
    /// (<paramref name="offsetX"/>, <paramref name="offsetY"/>) on the canvas.
    /// </summary>
    public Func<int, int, bool> AsClip(int offsetX = 0, int offsetY = 0) => (x, y) => Contains(x + offsetX, y + offsetY);
}
