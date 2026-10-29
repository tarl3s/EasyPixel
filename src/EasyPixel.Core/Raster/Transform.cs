using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

/// <summary>Whole-image transforms; each returns a new image and leaves the input untouched.</summary>
public static class Transform
{
    public static Image FlipH(Image src)
    {
        var d = new Image(src.Width, src.Height);
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
                d.Pixels[y * src.Width + x] = src.Pixels[y * src.Width + (src.Width - 1 - x)];
        return d;
    }

    public static Image FlipV(Image src)
    {
        var d = new Image(src.Width, src.Height);
        for (int y = 0; y < src.Height; y++)
            Array.Copy(src.Pixels, (src.Height - 1 - y) * src.Width, d.Pixels, y * src.Width, src.Width);
        return d;
    }

    /// <summary>90 degrees clockwise.</summary>
    public static Image Rotate90(Image src)
    {
        int w = src.Width, h = src.Height;
        var d = new Image(h, w);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                d.Pixels[x * h + (h - 1 - y)] = src.Pixels[y * w + x];
        return d;
    }

    public static Image Rotate180(Image src)
    {
        var d = new Image(src.Width, src.Height);
        int n = src.Pixels.Length;
        for (int i = 0; i < n; i++) d.Pixels[i] = src.Pixels[n - 1 - i];
        return d;
    }

    public static Image ScaleNearest(Image src, int w, int h)
    {
        var d = new Image(w, h);
        for (int y = 0; y < h; y++)
        {
            int sy = (int)((long)y * src.Height / h);
            for (int x = 0; x < w; x++)
                d.Pixels[y * w + x] = src.Pixels[sy * src.Width + (int)((long)x * src.Width / w)];
        }
        return d;
    }
}
