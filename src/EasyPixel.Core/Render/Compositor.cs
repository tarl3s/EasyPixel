using EasyPixel.Core.Model;

namespace EasyPixel.Core.Render;

/// <summary>Flattens the layers of one frame into an RGBA image.</summary>
public static class Compositor
{
    /// <summary>
    /// Clears <paramref name="area"/> of <paramref name="target"/> (same size as the sprite) and draws the frame there,
    /// layers bottom to top. Groups are composited apart and then blended with their own mode and opacity.
    /// Hidden layers (or layers in a hidden group) are skipped unless <paramref name="includeHidden"/>.
    /// Pass a <paramref name="scratch"/> you own to avoid sharing the per-thread cache. Output is always RGBA; indexed cels go through the palette.
    /// </summary>
    public static void Flatten(Sprite s, int frame, Image target, PixelRect area, bool includeHidden = false, CompositeScratch? scratch = null)
    {
        if (target.Width != s.Width || target.Height != s.Height)
            throw new ArgumentException("Target must have the sprite's size.", nameof(target));
        area = area.Intersect(new PixelRect(0, 0, s.Width, s.Height));
        if (area.IsEmpty) return;
        Clear(target, area);
        scratch ??= CompositeScratch.Shared;
        DrawChildren(s, frame, null, target, area, includeHidden, scratch, 0);
    }

    private static void Clear(Image img, PixelRect area)
    {
        for (int y = area.Y; y < area.Bottom; y++)
            Array.Clear(img.Pixels, y * img.Width + area.X, area.W);
    }

    private static void DrawChildren(Sprite s, int frame, Layer? parent, Image dest, PixelRect area, bool includeHidden, CompositeScratch scratch, int depth)
    {
        foreach (var layer in s.Layers)
        {
            if (layer.Parent != parent) continue;
            if (!layer.Visible && !includeHidden) continue;
            if (layer.IsGroup)
            {
                var buf = scratch.Group(depth, s.Width, s.Height);
                Clear(buf, area);
                DrawChildren(s, frame, layer, buf, area, includeHidden, scratch, depth + 1);
                BlendBuffer(buf, dest, area, layer.Blend, layer.Opacity);
            }
            else if (s.Cels.TryGetValue((layer, frame), out var cel))
                DrawCel(s, layer, cel, dest, area);
        }
    }

    private static void BlendBuffer(Image src, Image dest, PixelRect area, BlendMode mode, byte opacity)
    {
        for (int y = area.Y; y < area.Bottom; y++)
        {
            int row = y * dest.Width;
            for (int x = area.X; x < area.Right; x++)
            {
                uint f = src.Pixels[row + x];
                if ((f >> 24) != 0) dest.Pixels[row + x] = BlendModes.Blend(mode, dest.Pixels[row + x], f, opacity);
            }
        }
    }

    private static void DrawCel(Sprite s, Layer layer, Cel cel, Image dest, PixelRect area)
    {
        var r = area.Intersect(new PixelRect(cel.X, cel.Y, cel.Image.Width, cel.Image.Height));
        if (r.IsEmpty) return;
        byte opacity = (byte)Math.Clamp((int)MathF.Round(layer.Opacity * Math.Clamp(cel.Opacity, 0f, 1f)), 0, 255);
        if (opacity == 0) return;
        bool indexed = s.Mode == ColorMode.Indexed;
        var img = cel.Image;
        for (int y = r.Y; y < r.Bottom; y++)
        {
            int src = (y - cel.Y) * img.Width - cel.X, dst = y * dest.Width;
            for (int x = r.X; x < r.Right; x++)
            {
                uint p = img.Pixels[src + x];
                if (indexed) p = s.Palette.ToPacked(p);
                if ((p >> 24) != 0) dest.Pixels[dst + x] = BlendModes.Blend(layer.Blend, dest.Pixels[dst + x], p, opacity);
            }
        }
    }
}
