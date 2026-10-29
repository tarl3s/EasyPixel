namespace EasyPixel.Core.Model;

/// <summary>Pixel buffer. RGBA mode: <see cref="Rgba.Packed"/> values. Indexed mode: palette indices.</summary>
public sealed class Image
{
    public int Width { get; }
    public int Height { get; }
    public uint[] Pixels { get; }

    public Image(int width, int height, uint fill = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Pixels = new uint[checked(width * height)];
        if (fill != 0) Array.Fill(Pixels, fill);
    }

    /// <summary>Pixel value, or 0 outside the image.</summary>
    public uint Get(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height ? Pixels[y * Width + x] : 0;

    /// <summary>Sets a pixel; writes outside the image are ignored.</summary>
    public void Set(int x, int y, uint v)
    {
        if ((uint)x < (uint)Width && (uint)y < (uint)Height) Pixels[y * Width + x] = v;
    }

    public Image Clone()
    {
        var c = new Image(Width, Height);
        Array.Copy(Pixels, c.Pixels, Pixels.Length);
        return c;
    }
}

/// <summary>An image placed at (X, Y) on the canvas for one layer and frame.</summary>
public sealed class Cel
{
    public int X { get; set; }
    public int Y { get; set; }
    public Image Image { get; set; }
    public float Opacity { get; set; } = 1f;
    /// <summary>Format-specific data EasyPixel does not model, written back by the format that read it.</summary>
    public Dictionary<string, byte[]> Extra { get; } = new();

    public Cel(Image image, int x = 0, int y = 0)
    {
        Image = image;
        X = x;
        Y = y;
    }

    /// <summary>A separate copy: its own image, position, opacity and extra data.</summary>
    public Cel Clone()
    {
        var c = new Cel(Image.Clone(), X, Y) { Opacity = Opacity };
        foreach (var e in Extra) c.Extra[e.Key] = (byte[])e.Value.Clone();
        return c;
    }

    /// <summary>
    /// Grows the image (keeping its pixels where they are on the canvas) so it covers <paramref name="area"/> (canvas
    /// coordinates); the new part is <paramref name="fill"/>. Returns false when it already covered it.
    /// </summary>
    public bool GrowToCover(PixelRect area, uint fill)
    {
        var own = new PixelRect(X, Y, Image.Width, Image.Height);
        var need = own.Union(area);
        if (need == own) return false;
        var grown = new Image(need.W, need.H, fill);
        for (int y = 0; y < Image.Height; y++)
            Array.Copy(Image.Pixels, y * Image.Width, grown.Pixels, (y + Y - need.Y) * need.W + (X - need.X), Image.Width);
        Image = grown;
        X = need.X;
        Y = need.Y;
        return true;
    }
}
