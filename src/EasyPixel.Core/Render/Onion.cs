using EasyPixel.Core.Model;

namespace EasyPixel.Core.Render;

public sealed class OnionSettings
{
    public int Before { get; set; } = 1;
    public int After { get; set; } = 1;
    /// <summary>Opacity of the nearest ghost frame; further frames fade linearly.</summary>
    public byte Opacity { get; set; } = 68;
    /// <summary>Colour previous frames red-ish and next frames blue-ish.</summary>
    public bool Tint { get; set; }
}

/// <summary>Ghost images of the neighbouring frames, drawn by the UI beneath the current frame.</summary>
public static class Onion
{
    /// <summary>
    /// Clears <paramref name="target"/> (sprite size) and draws the <see cref="OnionSettings.Before"/> previous and
    /// <see cref="OnionSettings.After"/> next frames, farthest first. Frames outside the sprite are skipped (no wrap).
    /// </summary>
    public static void Render(Sprite s, int frame, OnionSettings o, Image target)
    {
        if (target.Width != s.Width || target.Height != s.Height)
            throw new ArgumentException("Target must have the sprite's size.", nameof(target));
        Array.Clear(target.Pixels);
        var whole = new PixelRect(0, 0, s.Width, s.Height);
        Image? tmp = null;
        for (int step = Math.Max(o.Before, o.After); step >= 1; step--)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int count = side < 0 ? o.Before : o.After;
                int f = frame + side * step;
                if (step > count || f < 0 || f >= s.Frames.Count) continue;
                tmp ??= new Image(s.Width, s.Height);
                Compositor.Flatten(s, f, tmp, whole);
                byte alpha = (byte)((o.Opacity * (count - step + 1) + count / 2) / count);
                uint tint = side < 0 ? 0xFF0000FFu : 0xFFFF0000u; // red / blue
                Draw(tmp, target, alpha, o.Tint ? tint : 0);
            }
        }
    }

    private static void Draw(Image src, Image dest, byte alpha, uint tint)
    {
        for (int i = 0; i < src.Pixels.Length; i++)
        {
            uint p = src.Pixels[i];
            if ((p >> 24) == 0) continue;
            if (tint != 0) p = Mix(p, tint);
            dest.Pixels[i] = BlendModes.Blend(BlendMode.Normal, dest.Pixels[i], p, alpha);
        }
    }

    /// <summary>Halfway between the pixel's colour and the tint, alpha kept.</summary>
    private static uint Mix(uint p, uint tint)
    {
        uint r = ((p & 0xFF) + (tint & 0xFF)) / 2;
        uint g = (((p >> 8) & 0xFF) + ((tint >> 8) & 0xFF)) / 2;
        uint b = (((p >> 16) & 0xFF) + ((tint >> 16) & 0xFF)) / 2;
        return (p & 0xFF000000u) | b << 16 | g << 8 | r;
    }
}
