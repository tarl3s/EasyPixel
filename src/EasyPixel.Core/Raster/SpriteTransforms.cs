using EasyPixel.Core.Model;

namespace EasyPixel.Core.Raster;

public enum SpriteRotation { Clockwise90, Half, CounterClockwise90 }

/// <summary>
/// Changes to the whole sprite: every cel of every layer and frame (linked cels once), positions included. None of
/// these record undo steps; wrap them in a SpriteStateEdit.
/// </summary>
public static class SpriteTransforms
{
    public static void Rotate(Sprite s, SpriteRotation r)
    {
        int w = s.Width, h = s.Height;
        foreach (var cel in Cels(s))
        {
            var img = cel.Image;
            int cw = img.Width, ch = img.Height, x = cel.X, y = cel.Y;
            switch (r)
            {
                case SpriteRotation.Clockwise90: // (px, py) -> (h - 1 - py, px)
                    cel.Image = Transform.Rotate90(img);
                    cel.X = h - (y + ch); cel.Y = x;
                    break;
                case SpriteRotation.Half:
                    cel.Image = Transform.Rotate180(img);
                    cel.X = w - (x + cw); cel.Y = h - (y + ch);
                    break;
                default: // (px, py) -> (py, w - 1 - px)
                    cel.Image = Transform.Rotate180(Transform.Rotate90(img));
                    cel.X = y; cel.Y = w - (x + cw);
                    break;
            }
        }
        if (r != SpriteRotation.Half) s.SetCanvasSize(h, w);
    }

    public static void Flip(Sprite s, bool horizontal)
    {
        foreach (var cel in Cels(s))
        {
            if (horizontal) { cel.Image = Transform.FlipH(cel.Image); cel.X = s.Width - (cel.X + cel.Image.Width); }
            else { cel.Image = Transform.FlipV(cel.Image); cel.Y = s.Height - (cel.Y + cel.Image.Height); }
        }
    }

    /// <summary>Scales the canvas and every cel to <paramref name="width"/> x <paramref name="height"/> (nearest neighbour).</summary>
    public static void Scale(Sprite s, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        double sx = (double)width / s.Width, sy = (double)height / s.Height;
        foreach (var cel in Cels(s))
        {
            int x0 = (int)Math.Round(cel.X * sx), y0 = (int)Math.Round(cel.Y * sy);
            int x1 = (int)Math.Round((cel.X + cel.Image.Width) * sx), y1 = (int)Math.Round((cel.Y + cel.Image.Height) * sy);
            cel.Image = Transform.ScaleNearest(cel.Image, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));
            cel.X = x0; cel.Y = y0;
        }
        s.SetCanvasSize(width, height);
    }

    /// <summary>
    /// Limits the sprite to at most <paramref name="colors"/> colours chosen by median cut over every cel. RGBA sprites
    /// keep their mode and get the new colours as their palette; indexed sprites get the new palette (plus the
    /// transparent entry) and their pixels renumbered.
    /// </summary>
    public static void ReduceColors(Sprite s, int colors)
    {
        bool indexed = s.Mode == ColorMode.Indexed;
        if (indexed) s.ConvertMode(ColorMode.Rgba);
        var cels = Cels(s).ToList();
        var counts = new Dictionary<uint, int>(); // counted cel by cel: no copy of the whole picture
        foreach (var c in cels) Quantize.CountColors(c.Image, counts);
        var pal = Quantize.MedianCut(counts, colors);
        foreach (var c in cels) c.Image = Quantize.ToPalette(c.Image, pal, dither: false);
        if (indexed) s.ConvertMode(ColorMode.Indexed);
        else
        {
            s.Palette.Colors.Clear();
            s.Palette.Colors.AddRange(pal.Colors);
            s.Palette.TransparentIndex = -1;
        }
    }

    private static IEnumerable<Cel> Cels(Sprite s) => s.Cels.Values.Distinct().ToList();
}
