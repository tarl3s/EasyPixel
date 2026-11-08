using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Model;

namespace EasyPixel.Ai;

/// <summary>Reads the picture a generator sent (PNG or JPEG) into RGBA pixels. Core has no image decoder for JPEG, Avalonia does.</summary>
public static class ImageDecode
{
    public static Image Load(byte[] data)
    {
        WriteableBitmap bmp;
        try
        {
            using var ms = new MemoryStream(data ?? []);
            bmp = WriteableBitmap.Decode(ms);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { throw new AiException("The picture couldn't be read."); }
        using (bmp)
        {
            int w = bmp.PixelSize.Width, h = bmp.PixelSize.Height;
            if (w < 1 || h < 1) throw new AiException("The picture couldn't be read.");
            var img = new Image(w, h);
            using var fb = bmp.Lock();
            bool bgra = bmp.Format == PixelFormats.Bgra8888;
            bool premul = bmp.AlphaFormat == AlphaFormat.Premul;
            unsafe
            {
                for (int y = 0; y < h; y++)
                {
                    var row = new ReadOnlySpan<byte>((byte*)fb.Address + (long)y * fb.RowBytes, w * 4);
                    for (int x = 0; x < w; x++)
                    {
                        byte c0 = row[x * 4], c1 = row[x * 4 + 1], c2 = row[x * 4 + 2], a = row[x * 4 + 3];
                        byte r = bgra ? c2 : c0, g = c1, b = bgra ? c0 : c2;
                        if (premul && a is > 0 and < 255)
                        {
                            r = (byte)Math.Min(255, r * 255 / a); g = (byte)Math.Min(255, g * 255 / a); b = (byte)Math.Min(255, b * 255 / a);
                        }
                        img.Pixels[y * w + x] = a == 0 ? 0u : new Rgba(r, g, b, a).Packed;
                    }
                }
            }
            return img;
        }
    }
}
