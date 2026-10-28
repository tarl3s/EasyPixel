namespace EasyPixel.Core.Raster;

/// <summary>
/// Mirror drawing. The axes are in pixel-edge coordinates: AxisX = 4 on an 8-wide canvas is the line between
/// pixels 3 and 4; AxisX = 3.5 is the middle of pixel 3.
/// </summary>
public sealed class Symmetry
{
    public bool H { get; set; }
    public bool V { get; set; }
    public float AxisX { get; set; }
    public float AxisY { get; set; }

    /// <summary>The point itself plus its mirrors, without repeats (a point on an axis mirrors onto itself).</summary>
    public IEnumerable<(int, int)> Mirror(int x, int y)
    {
        Span<(int, int)> buf = stackalloc (int, int)[4];
        int n = Expand(x, y, buf);
        var list = new (int, int)[n];
        for (int i = 0; i < n; i++) list[i] = buf[i];
        return list;
    }

    /// <summary>Allocation-free <see cref="Mirror"/>: fills <paramref name="buf"/> (length 4) and returns the count.</summary>
    internal int Expand(int x, int y, Span<(int, int)> buf)
    {
        int n = 0;
        buf[n++] = (x, y);
        int mx = MirrorX(x), my = MirrorY(y);
        if (H && mx != x) buf[n++] = (mx, y);
        if (V && my != y) buf[n++] = (x, my);
        if (H && V && mx != x && my != y) buf[n++] = (mx, my);
        return n;
    }

    private int MirrorX(int x) => (int)Math.Floor(2.0 * AxisX - 1 - x + 0.5);
    private int MirrorY(int y) => (int)Math.Floor(2.0 * AxisY - 1 - y + 0.5);
}
