namespace EasyPixel.Core.Raster;

public enum BrushShape { Square, Circle }

/// <summary>A stamp of <see cref="Size"/> x <see cref="Size"/> pixels centred on the cursor (even sizes lean up-left).</summary>
public sealed class Brush
{
    public int Size { get; set; } = 1;
    public BrushShape Shape { get; set; } = BrushShape.Square;

    public Brush() { }
    public Brush(int size, BrushShape shape = BrushShape.Square) { Size = size; Shape = shape; }

    /// <summary>Calls <paramref name="op"/> for every pixel the brush covers when centred on (x, y).</summary>
    public static void ForEach(int x, int y, Brush b, Action<int, int> op)
    {
        int s = Math.Max(1, b.Size);
        int left = x - (s - 1) / 2, top = y - (s - 1) / 2;
        double c = (s - 1) / 2.0, r2 = c * c + 0.25;
        for (int j = 0; j < s; j++)
            for (int i = 0; i < s; i++)
            {
                if (b.Shape == BrushShape.Circle && s > 2)
                {
                    double dx = i - c, dy = j - c;
                    if (dx * dx + dy * dy > r2) continue;
                }
                op(left + i, top + j);
            }
    }

    /// <summary>Paints the brush at (x, y) and at its mirrors when <paramref name="sym"/> is given.</summary>
    public static void Stamp(PixelSink s, int x, int y, uint color, Brush b, Symmetry? sym = null)
    {
        if (sym == null) { StampOne(s, x, y, color, b); return; }
        Span<(int, int)> pts = stackalloc (int, int)[4];
        int n = sym.Expand(x, y, pts);
        for (int k = 0; k < n; k++) StampOne(s, pts[k].Item1, pts[k].Item2, color, b);
    }

    private static void StampOne(PixelSink s, int x, int y, uint color, Brush b)
    {
        if (b.Size <= 1) { s.Set(x, y, color); return; }
        ForEach(x, y, b, (px, py) => s.Set(px, py, color));
    }
}
