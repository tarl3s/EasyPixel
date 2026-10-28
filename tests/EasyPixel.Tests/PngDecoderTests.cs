using System.IO.Compression;
using EasyPixel.Core.Formats;

namespace EasyPixel.Tests;

/// <summary>Decoder fixtures built by a test-only writer that shares no code with <see cref="Png"/>.</summary>
public class PngDecoderTests
{
    private static uint Crc(byte[] data)
    {
        uint c = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320 : c >> 1;
        }
        return ~c;
    }

    private static void Be(Stream s, uint v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }

    private static void Chunk(Stream s, string type, byte[] body)
    {
        Be(s, (uint)body.Length);
        var td = new byte[4 + body.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)type[i];
        body.CopyTo(td, 4);
        s.Write(td);
        Be(s, Crc(td));
    }

    private static byte[] PackRow(int[] samples, int depth)
    {
        if (depth >= 8)
        {
            int per = depth / 8;
            var r = new byte[samples.Length * per];
            for (int i = 0; i < samples.Length; i++)
                for (int k = 0; k < per; k++) r[i * per + k] = (byte)(samples[i] >> (8 * (per - 1 - k)));
            return r;
        }
        var row = new byte[(samples.Length * depth + 7) / 8];
        for (int i = 0; i < samples.Length; i++) row[i * depth / 8] |= (byte)(samples[i] << (8 - depth - i * depth % 8));
        return row;
    }

    private static byte[] FilterRow(int type, byte[] cur, byte[] prev, int bpp)
    {
        var o = new byte[cur.Length];
        for (int i = 0; i < cur.Length; i++)
        {
            int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
            int pred = type switch
            {
                0 => 0, 1 => a, 2 => b, 3 => (a + b) >> 1,
                _ => PaethPick(a, b, c),
            };
            o[i] = (byte)(cur[i] - pred);
        }
        return o;
    }

    private static int PaethPick(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static readonly (int x0, int y0, int dx, int dy)[] Adam7 =
        { (0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2) };

    /// <param name="samples">samples[y][x * channels + c]</param>
    private static byte[] Build(int w, int h, int depth, int ctype, bool interlaced, int[][] samples, int channels,
        byte[]? plte = null, byte[]? trns = null, Func<int, int>? filterForRow = null)
    {
        int bpp = Math.Max(1, channels * depth / 8);
        var raw = new MemoryStream();
        var passes = interlaced ? Adam7 : new[] { (0, 0, 1, 1) };
        foreach (var (x0, y0, dx, dy) in passes)
        {
            var xs = Enumerable.Range(0, w).Where(x => x >= x0 && (x - x0) % dx == 0).ToArray();
            var ys = Enumerable.Range(0, h).Where(y => y >= y0 && (y - y0) % dy == 0).ToArray();
            if (xs.Length == 0 || ys.Length == 0) continue;
            byte[]? prev = null;
            int rowNo = 0;
            foreach (int y in ys)
            {
                var s = xs.SelectMany(x => Enumerable.Range(0, channels).Select(c => samples[y][x * channels + c])).ToArray();
                var cur = PackRow(s, depth);
                prev ??= new byte[cur.Length];
                int ft = filterForRow?.Invoke(rowNo++) ?? 0;
                raw.WriteByte((byte)ft);
                raw.Write(FilterRow(ft, cur, prev, bpp));
                prev = cur;
            }
        }
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Fastest, leaveOpen: true)) zs.Write(raw.ToArray());

        var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new MemoryStream();
        Be(ihdr, (uint)w); Be(ihdr, (uint)h);
        ihdr.Write(new byte[] { (byte)depth, (byte)ctype, 0, 0, (byte)(interlaced ? 1 : 0) });
        Chunk(png, "IHDR", ihdr.ToArray());
        if (plte != null) Chunk(png, "PLTE", plte);
        if (trns != null) Chunk(png, "tRNS", trns);
        Chunk(png, "IDAT", z.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static uint P(int r, int g, int b, int a) => (uint)(a << 24 | b << 16 | g << 8 | r);

    private static int[][] Grid(int w, int h, int channels, int max, int seed)
    {
        var rnd = new Random(seed);
        return Enumerable.Range(0, h).Select(_ => Enumerable.Range(0, w * channels).Select(_ => rnd.Next(max + 1)).ToArray()).ToArray();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rgba8_AllPassesAndFilters(bool interlaced)
    {
        var s = Grid(9, 7, 4, 255, 1);
        var img = Png.Decode(Build(9, 7, 8, 6, interlaced, s, 4, filterForRow: r => r % 5));
        Assert.Equal((9, 7), (img.Width, img.Height));
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 9; x++)
                Assert.Equal(P(s[y][x * 4], s[y][x * 4 + 1], s[y][x * 4 + 2], s[y][x * 4 + 3]), img.Get(x, y));
    }

    [Fact]
    public void Adam7_Tiny_SizesWithEmptyPasses()
    {
        foreach (var (w, h) in new[] { (1, 1), (2, 1), (1, 3), (5, 2) })
        {
            var s = Grid(w, h, 4, 255, w * 10 + h);
            var img = Png.Decode(Build(w, h, 8, 6, true, s, 4, filterForRow: r => r % 5));
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    Assert.Equal(P(s[y][x * 4], s[y][x * 4 + 1], s[y][x * 4 + 2], s[y][x * 4 + 3]), img.Get(x, y));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Palette_LowBitDepths_WithPartialTrns(int depth)
    {
        int n = 1 << depth;
        var plte = new byte[n * 3];
        for (int i = 0; i < n; i++) { plte[i * 3] = (byte)(i * 15 + 1); plte[i * 3 + 1] = (byte)(200 - i * 9); plte[i * 3 + 2] = (byte)(i * 3); }
        var trns = n >= 4 ? new byte[] { 0, 128 } : new byte[] { 40 }; // shorter than the palette: the rest is opaque
        var s = Grid(7, 5, 1, n - 1, depth);
        foreach (bool inter in new[] { false, true })
        {
            var img = Png.Decode(Build(7, 5, depth, 3, inter, s, 1, plte, trns, r => (r % 4) + 1));
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 7; x++)
                {
                    int i = s[y][x];
                    Assert.Equal(P(plte[i * 3], plte[i * 3 + 1], plte[i * 3 + 2], i < trns.Length ? trns[i] : 255), img.Get(x, y));
                }
        }
    }

    [Fact]
    public void Rgba16_TakesHighByte()
    {
        var s = Grid(6, 4, 4, 65535, 9);
        var img = Png.Decode(Build(6, 4, 16, 6, false, s, 4, filterForRow: r => (r % 4) + 1));
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 6; x++)
                Assert.Equal(P(s[y][x * 4] >> 8, s[y][x * 4 + 1] >> 8, s[y][x * 4 + 2] >> 8, s[y][x * 4 + 3] >> 8), img.Get(x, y));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GreyAlpha8(bool interlaced)
    {
        var s = Grid(8, 6, 2, 255, 4);
        var img = Png.Decode(Build(8, 6, 8, 4, interlaced, s, 2, filterForRow: r => r % 5));
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 8; x++)
                Assert.Equal(P(s[y][x * 2], s[y][x * 2], s[y][x * 2], s[y][x * 2 + 1]), img.Get(x, y));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EachFilterType_ForcedOnEveryRow(int filter)
    {
        var rgb = Grid(11, 6, 3, 255, filter);
        var img = Png.Decode(Build(11, 6, 8, 2, false, rgb, 3, filterForRow: _ => filter));
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 11; x++)
                Assert.Equal(P(rgb[y][x * 3], rgb[y][x * 3 + 1], rgb[y][x * 3 + 2], 255), img.Get(x, y));

        var s16 = Grid(5, 5, 4, 65535, filter + 20);
        var img16 = Png.Decode(Build(5, 5, 16, 6, false, s16, 4, filterForRow: _ => filter));
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                Assert.Equal(P(s16[y][x * 4] >> 8, s16[y][x * 4 + 1] >> 8, s16[y][x * 4 + 2] >> 8, s16[y][x * 4 + 3] >> 8), img16.Get(x, y));
    }
}
