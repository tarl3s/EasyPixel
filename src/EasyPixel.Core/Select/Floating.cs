using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;

namespace EasyPixel.Core.Select;

/// <summary>
/// Pixels lifted out of a cel so they can be moved, transformed, copied or pasted. (X, Y) is the top-left corner in
/// canvas coordinates. Pixels equal to <see cref="Clear"/> (the sprite's transparent value: 0 in RGBA, the transparent
/// palette index in indexed mode) are holes: lifting leaves that value behind and dropping doesn't write it.
/// </summary>
public sealed class Floating
{
    public Image Pixels { get; private set; }
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>The value that means "no pixel" (see the class summary).</summary>
    public uint Clear { get; }

    public Floating(Image pixels, int x, int y, uint clear = 0)
    {
        Pixels = pixels;
        X = x;
        Y = y;
        Clear = clear;
    }

    /// <summary>
    /// Lifts the selected pixels out of the cel (they become <paramref name="clear"/> in it); <paramref name="affected"/>
    /// is the cleared area in cel coordinates.
    /// </summary>
    public static Floating Lift(Cel c, Selection s, out PixelRect affected, uint clear = 0)
    {
        var b = s.Bounds;
        if (b.IsEmpty)
        {
            affected = default;
            return new Floating(new Image(1, 1, clear), 0, 0, clear);
        }
        var img = new Image(b.W, b.H, clear);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = b.Y; y < b.Bottom; y++)
            for (int x = b.X; x < b.Right; x++)
            {
                if (!s.Contains(x, y)) continue;
                int cx = x - c.X, cy = y - c.Y;
                if ((uint)cx >= (uint)c.Image.Width || (uint)cy >= (uint)c.Image.Height) continue;
                img.Pixels[(y - b.Y) * b.W + (x - b.X)] = c.Image.Pixels[cy * c.Image.Width + cx];
                c.Image.Pixels[cy * c.Image.Width + cx] = clear;
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
                if (cy < minY) minY = cy;
                maxY = cy;
            }
        affected = maxX < 0 ? default : new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return new Floating(img, b.X, b.Y, clear);
    }

    public static Floating Lift(Cel c, Selection s, uint clear = 0) => Lift(c, s, out _, clear);

    /// <summary>
    /// Writes the pixels that aren't <see cref="Clear"/> into the cel, growing the cel (and moving its X/Y) when they fall
    /// outside it; grown area is <see cref="Clear"/>. Returns the rectangle written, in the cel's coordinates after any growth.
    /// </summary>
    public PixelRect Drop(Cel c)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < Pixels.Height; y++)
            for (int x = 0; x < Pixels.Width; x++)
                if (Pixels.Pixels[y * Pixels.Width + x] != Clear)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    maxY = y;
                }
        if (maxX < 0) return default;

        var canvasRect = new PixelRect(X + minX, Y + minY, maxX - minX + 1, maxY - minY + 1);
        c.GrowToCover(canvasRect, Clear);
        var img = c.Image;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                uint v = Pixels.Pixels[y * Pixels.Width + x];
                if (v != Clear) img.Pixels[(Y + y - c.Y) * img.Width + (X + x - c.X)] = v;
            }
        return new PixelRect(canvasRect.X - c.X, canvasRect.Y - c.Y, canvasRect.W, canvasRect.H);
    }

    public void FlipH() => Pixels = Transform.FlipH(Pixels);
    public void FlipV() => Pixels = Transform.FlipV(Pixels);
    /// <summary>Turns the pixels a quarter turn (clockwise by default) around their centre.</summary>
    public void Rotate90(bool clockwise = true)
    {
        int w = Pixels.Width, h = Pixels.Height;
        Pixels = clockwise ? Transform.Rotate90(Pixels) : Transform.Rotate180(Transform.Rotate90(Pixels));
        X += (w - h) / 2;
        Y += (h - w) / 2;
    }

    /// <summary>Scales the pixels to <paramref name="w"/> x <paramref name="h"/> (nearest neighbour) around their centre.</summary>
    public void Scale(int w, int h)
    {
        X += (Pixels.Width - w) / 2;
        Y += (Pixels.Height - h) / 2;
        Pixels = Transform.ScaleNearest(Pixels, w, h);
    }
}
