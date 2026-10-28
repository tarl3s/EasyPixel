namespace EasyPixel.Core.Model;

public readonly record struct PixelRect(int X, int Y, int W, int H)
{
    public int Right => X + W;
    public int Bottom => Y + H;
    public bool IsEmpty => W <= 0 || H <= 0;

    public bool Contains(int x, int y) => x >= X && y >= Y && x < Right && y < Bottom;

    /// <summary>Overlap of the two rectangles; an empty rectangle when they do not touch.</summary>
    public PixelRect Intersect(PixelRect o)
    {
        int x = Math.Max(X, o.X), y = Math.Max(Y, o.Y);
        int r = Math.Min(Right, o.Right), b = Math.Min(Bottom, o.Bottom);
        return r <= x || b <= y ? default : new PixelRect(x, y, r - x, b - y);
    }

    /// <summary>Smallest rectangle holding both; empty rectangles are ignored.</summary>
    public PixelRect Union(PixelRect o)
    {
        if (IsEmpty) return o;
        if (o.IsEmpty) return this;
        int x = Math.Min(X, o.X), y = Math.Min(Y, o.Y);
        return new PixelRect(x, y, Math.Max(Right, o.Right) - x, Math.Max(Bottom, o.Bottom) - y);
    }
}
