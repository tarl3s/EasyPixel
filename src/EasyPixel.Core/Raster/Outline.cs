namespace EasyPixel.Core.Raster;

public static class Outline
{
    /// <summary>
    /// Outside: transparent pixels touching an opaque one take <paramref name="color"/>. Inside: opaque pixels touching a
    /// transparent one (or the image edge) do. Neighbours are the 4 sides, plus the diagonals when <paramref name="diagonal"/>.
    /// Transparent means alpha 0 unless <paramref name="isTransparent"/> says otherwise (use it for indexed images).
    /// </summary>
    public static void Apply(PixelSink s, uint color, bool outside, bool diagonal, Func<uint, bool>? isTransparent = null)
    {
        isTransparent ??= v => (v >> 24) == 0;
        var img = s.Image;
        int w = img.Width, h = img.Height;
        var src = (uint[])img.Pixels.Clone(); // decide on the original pixels, not on the outline being drawn
        bool Clear(int x, int y) => (uint)x >= (uint)w || (uint)y >= (uint)h || isTransparent(src[y * w + x]);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool transparent = isTransparent(src[y * w + x]);
                if (transparent == !outside) continue; // outside edits transparent pixels, inside edits opaque ones
                bool touches = false;
                for (int dy = -1; dy <= 1 && !touches; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx == 0 && dy == 0) || (!diagonal && dx != 0 && dy != 0)) continue;
                        int nx = x + dx, ny = y + dy;
                        // Outside: an in-image opaque neighbour. Inside: a transparent or out-of-image neighbour.
                        if (outside ? ((uint)nx < (uint)w && (uint)ny < (uint)h && !Clear(nx, ny)) : Clear(nx, ny)) { touches = true; break; }
                    }
                if (touches) s.Set(x, y, color);
            }
    }
}
