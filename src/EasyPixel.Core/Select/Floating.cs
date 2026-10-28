using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Select;

/// <summary>
/// Pixels lifted out of a cel so they can be moved, transformed, copied or pasted. (X, Y) is the top-left corner in
/// canvas coordinates; 0 pixels are transparent and are not written when dropped.
/// </summary>
public sealed class Floating
{
    public Image Pixels { get; private set; }
    public int X { get; set; }
    public int Y { get; set; }

    public Floating(Image pixels, int x, int y)
    {
        Pixels = pixels;
        X = x;
        Y = y;
    }

    /// <summary>Lifts the selected pixels out of the cel (they become 0 in it); <paramref name="affected"/> is the cleared area in cel coordinates.</summary>
    public static Floating Lift(Cel c, Selection s, out PixelRect affected)
    {
        var b = s.Bounds;
        if (b.IsEmpty)
        {
            affected = default;
            return new Floating(new Image(1, 1), 0, 0);
        }
        var img = new Image(b.W, b.H);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = b.Y; y < b.Bottom; y++)
            for (int x = b.X; x < b.Right; x++)
            {
                if (!s.Contains(x, y)) continue;
                int cx = x - c.X, cy = y - c.Y;
                if ((uint)cx >= (uint)c.Image.Width || (uint)cy >= (uint)c.Image.Height) continue;
                img.Pixels[(y - b.Y) * b.W + (x - b.X)] = c.Image.Pixels[cy * c.Image.Width + cx];
                c.Image.Pixels[cy * c.Image.Width + cx] = 0;
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
                if (cy < minY) minY = cy;
                maxY = cy;
            }
        affected = maxX < 0 ? default : new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return new Floating(img, b.X, b.Y);
    }

    public static Floating Lift(Cel c, Selection s) => Lift(c, s, out _);

    /// <summary>
    /// Writes the non-transparent pixels into the cel, growing the cel (and moving its X/Y) when they fall outside it.
    /// Returns the rectangle written, in the cel's coordinates after any growth.
    /// </summary>
    public PixelRect Drop(Cel c)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < Pixels.Height; y++)
            for (int x = 0; x < Pixels.Width; x++)
                if (Pixels.Pixels[y * Pixels.Width + x] != 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    maxY = y;
                }
        if (maxX < 0) return default;

        var canvasRect = new PixelRect(X + minX, Y + minY, maxX - minX + 1, maxY - minY + 1);
        var celRect = new PixelRect(c.X, c.Y, c.Image.Width, c.Image.Height);
        var need = celRect.Union(canvasRect);
        if (need != celRect)
        {
            var grown = new Image(need.W, need.H);
            for (int y = 0; y < c.Image.Height; y++)
                Array.Copy(c.Image.Pixels, y * c.Image.Width, grown.Pixels, (y + c.Y - need.Y) * need.W + (c.X - need.X), c.Image.Width);
            c.Image = grown;
            c.X = need.X;
            c.Y = need.Y;
        }
        var img = c.Image;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                uint v = Pixels.Pixels[y * Pixels.Width + x];
                if (v != 0) img.Pixels[(Y + y - c.Y) * img.Width + (X + x - c.X)] = v;
            }
        return new PixelRect(canvasRect.X - c.X, canvasRect.Y - c.Y, canvasRect.W, canvasRect.H);
    }

    public void FlipH() => Pixels = Transform.FlipH(Pixels);
    public void FlipV() => Pixels = Transform.FlipV(Pixels);
    public void Rotate90() => Pixels = Transform.Rotate90(Pixels);
    public void Scale(int w, int h) => Pixels = Transform.ScaleNearest(Pixels, w, h);
}
