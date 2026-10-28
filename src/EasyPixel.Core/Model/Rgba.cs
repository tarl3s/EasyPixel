namespace EasyPixel.Core.Model;

/// <summary>An 8-bit-per-channel colour with straight (non-premultiplied) alpha.</summary>
public readonly record struct Rgba(byte R, byte G, byte B, byte A)
{
    public static Rgba Transparent => new(0, 0, 0, 0);

    /// <summary>
    /// Packed layout 0xAABBGGRR: R is the lowest byte, so the bytes in memory (little-endian) read R,G,B,A.
    /// This is what <see cref="Image.Pixels"/> stores in RGBA mode.
    /// </summary>
    public uint Packed => (uint)(A << 24 | B << 16 | G << 8 | R);

    public static Rgba FromPacked(uint p) => new((byte)p, (byte)(p >> 8), (byte)(p >> 16), (byte)(p >> 24));

    /// <summary>Squared distance over all four channels.</summary>
    public int DistanceSquared(Rgba o)
    {
        int r = R - o.R, g = G - o.G, b = B - o.B, a = A - o.A;
        return r * r + g * g + b * b + a * a;
    }
}
