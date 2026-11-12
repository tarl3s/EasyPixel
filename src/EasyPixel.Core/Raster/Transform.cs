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

    /// <summary>
    /// Turns the picture by any angle (degrees, clockwise) without blending: every pixel takes the colour of the source
    /// pixel under its centre, so the result stays crisp pixel art. The result is as big as the turned picture's box;
    /// corners nothing lands on get <paramref name="clear"/>. Exact quarter turns use the lossless versions.
    /// </summary>
    public static Image RotateNearest(Image src, double degrees, uint clear = 0)
    {
        double a = degrees % 360;
        if (a < 0) a += 360;
        if (Math.Abs(a) < 1e-9) return src.Clone();
        if (Math.Abs(a - 90) < 1e-9) return Rotate90(src);
        if (Math.Abs(a - 180) < 1e-9) return Rotate180(src);
        if (Math.Abs(a - 270) < 1e-9) return Rotate180(Rotate90(src));
        double r = a * Math.PI / 180, cos = Math.Cos(r), sin = Math.Sin(r);
        int w = src.Width, h = src.Height;
        int nw = Math.Max(1, (int)Math.Ceiling(Math.Abs(w * cos) + Math.Abs(h * sin) - 1e-6));
        int nh = Math.Max(1, (int)Math.Ceiling(Math.Abs(w * sin) + Math.Abs(h * cos) - 1e-6));
        var d = new Image(nw, nh, clear);
        double cx = w / 2.0, cy = h / 2.0, ncx = nw / 2.0, ncy = nh / 2.0;
        for (int y = 0; y < nh; y++)
            for (int x = 0; x < nw; x++)
            {
                // back from the destination pixel's centre to the source (the inverse turn)
                double dx = x + 0.5 - ncx, dy = y + 0.5 - ncy;
                int sx = (int)Math.Floor(dx * cos + dy * sin + cx), sy = (int)Math.Floor(-dx * sin + dy * cos + cy);
                if ((uint)sx < (uint)w && (uint)sy < (uint)h) d.Pixels[y * nw + x] = src.Pixels[sy * w + sx];
            }
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
