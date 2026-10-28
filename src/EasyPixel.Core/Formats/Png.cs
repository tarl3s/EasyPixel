using System.IO.Compression;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>
/// PNG codec without dependencies. <see cref="Encode"/> writes 8-bit RGBA; <see cref="Decode"/> reads colour types
/// 0, 2, 3, 4 and 6 (bit depths 1/2/4/8, 16 by taking the high byte), tRNS, all filters and Adam7, always returning RGBA.
/// Any malformed input throws <see cref="FormatException"/>.
/// </summary>
public static class Png
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private const long MaxPixels = 1L << 27; // refuse absurd sizes before allocating

    // ---- encoding ----

    /// <summary>8-bit RGBA (colour type 6), per-row filter chosen by the smallest sum of absolute values.</summary>
    public static byte[] Encode(Image img)
    {
        var raw = new byte[img.Width * 4];
        return Write(img.Width, img.Height, 6, 4, y =>
        {
            for (int x = 0; x < img.Width; x++)
            {
                uint p = img.Pixels[y * img.Width + x];
                raw[x * 4] = (byte)p; raw[x * 4 + 1] = (byte)(p >> 8); raw[x * 4 + 2] = (byte)(p >> 16); raw[x * 4 + 3] = (byte)(p >> 24);
            }
            return raw;
        });
    }

    /// <summary>
    /// 8-bit greyscale (colour type 0), value = the low byte of each pixel. Used for indexed cels so palette indices stay lossless.
    /// </summary>
    public static byte[] EncodeGrey8(Image img)
    {
        var raw = new byte[img.Width];
        return Write(img.Width, img.Height, 0, 1, y =>
        {
            for (int x = 0; x < img.Width; x++) raw[x] = (byte)img.Pixels[y * img.Width + x];
            return raw;
        });
    }

    /// <summary>Reads a PNG written by <see cref="EncodeGrey8"/> (any PNG works; the value is the red channel).</summary>
    public static Image DecodeGrey8(byte[] data)
    {
        var img = Decode(data);
        for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] &= 0xFF;
        return img;
    }

    private static byte[] Write(int width, int height, byte colorType, int bpp, Func<int, byte[]> row)
    {
        var idat = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionLevel.Optimal, true))
        {
            int stride = width * bpp;
            var prev = new byte[stride];
            var cur = new byte[stride];
            var best = new byte[stride + 1];
            var cand = new byte[stride + 1];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(row(y), cur, stride);
                long bestScore = long.MaxValue;
                for (int f = 0; f < 5; f++)
                {
                    cand[0] = (byte)f;
                    long score = 0;
                    for (int i = 0; i < stride; i++)
                    {
                        int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                        int pred = f switch { 0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2, _ => Paeth(a, b, c) };
                        byte v = (byte)(cur[i] - pred);
                        cand[i + 1] = v;
                        score += v < 128 ? v : 256 - v;
                    }
                    if (score < bestScore) { bestScore = score; (best, cand) = (cand, best); }
                }
                z.Write(best, 0, stride + 1);
                (prev, cur) = (cur, prev);
            }
        }

        var o = new MemoryStream();
        o.Write(Signature);
        var ihdr = new byte[13];
        WriteBE(ihdr, 0, (uint)width); WriteBE(ihdr, 4, (uint)height);
        ihdr[8] = 8; ihdr[9] = colorType;
        Chunk(o, "IHDR", ihdr);
        Chunk(o, "IDAT", idat.ToArray());
        Chunk(o, "IEND", Array.Empty<byte>());
        return o.ToArray();
    }

    private static void Chunk(Stream o, string type, byte[] data)
    {
        var buf = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) buf[i] = (byte)type[i];
        data.CopyTo(buf, 4);
        var len = new byte[4];
        WriteBE(len, 0, (uint)data.Length);
        o.Write(len);
        o.Write(buf);
        WriteBE(len, 0, Crc32(buf));
        o.Write(len);
    }

    private static void WriteBE(byte[] b, int at, uint v)
    {
        b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v;
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint c = 0xFFFFFFFFu;
        foreach (byte b in data) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    // ---- decoding ----

    /// <summary>Decodes any supported PNG to RGBA <see cref="Rgba.Packed"/> pixels.</summary>
    public static Image Decode(byte[] data)
    {
        try { return DecodeCore(data); }
        catch (FormatException) { throw; }
        catch (Exception e) when (e is InvalidDataException or EndOfStreamException or IndexOutOfRangeException
                                      or ArgumentException or OverflowException or InvalidOperationException or OutOfMemoryException)
        {
            throw new FormatException("The PNG data is damaged: " + e.Message, e);
        }
    }

    private static Image DecodeCore(byte[] data)
    {
        if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(Signature)) throw new FormatException("This is not a PNG file.");
        int pos = 8;
        int w = 0, h = 0, depth = 0, ctype = 0, interlace = 0;
        bool sawHeader = false, sawEnd = false;
        byte[]? plte = null, trns = null;
        var idat = new MemoryStream();
        while (pos < data.Length && !sawEnd)
        {
            if (data.Length - pos < 12) throw new FormatException("The PNG file is cut short.");
            uint len = ReadBE(data, pos);
            if (len > data.Length - pos - 12) throw new FormatException("The PNG file is cut short.");
            string type = new string(new[] { (char)data[pos + 4], (char)data[pos + 5], (char)data[pos + 6], (char)data[pos + 7] });
            var body = data.AsSpan(pos + 8, (int)len);
            if (Crc32(data.AsSpan(pos + 4, (int)len + 4)) != ReadBE(data, pos + 8 + (int)len))
                throw new FormatException($"The PNG file is damaged (bad checksum in {type}).");
            switch (type)
            {
                case "IHDR":
                    if (len != 13) throw new FormatException("The PNG header is invalid.");
                    w = (int)Math.Min(ReadBE(data, pos + 8), int.MaxValue);
                    h = (int)Math.Min(ReadBE(data, pos + 12), int.MaxValue);
                    depth = body[8]; ctype = body[9]; interlace = body[12];
                    if (w <= 0 || h <= 0 || (long)w * h > MaxPixels) throw new FormatException("The PNG image size is not supported.");
                    if (body[10] != 0 || body[11] != 0 || interlace > 1) throw new FormatException("The PNG uses an unsupported method.");
                    bool ok = ctype switch
                    {
                        0 => depth is 1 or 2 or 4 or 8 or 16,
                        2 or 4 or 6 => depth is 8 or 16,
                        3 => depth is 1 or 2 or 4 or 8,
                        _ => false,
                    };
                    if (!ok) throw new FormatException($"PNG colour type {ctype} with {depth} bits is not supported.");
                    sawHeader = true;
                    break;
                case "PLTE": plte = body.ToArray(); break;
                case "tRNS": trns = body.ToArray(); break;
                case "IDAT": idat.Write(body); break;
                case "IEND": sawEnd = true; break;
            }
            pos += 12 + (int)len;
        }
        if (!sawHeader) throw new FormatException("The PNG header is missing.");
        if (!sawEnd) throw new FormatException("The PNG file is cut short.");
        if (ctype == 3 && (plte == null || plte.Length == 0 || plte.Length % 3 != 0)) throw new FormatException("The PNG palette is missing or invalid.");

        int channels = ctype switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, _ => 4 };
        int bitsPerPixel = channels * depth;
        int bpp = Math.Max(1, bitsPerPixel / 8);

        // Pass layout: (x0, y0, dx, dy); a plain image is one pass covering everything.
        int[][] passes = interlace == 0
            ? new[] { new[] { 0, 0, 1, 1 } }
            : new[] { new[] { 0, 0, 8, 8 }, new[] { 4, 0, 8, 8 }, new[] { 0, 4, 4, 8 }, new[] { 2, 0, 4, 4 }, new[] { 0, 2, 2, 4 }, new[] { 1, 0, 2, 2 }, new[] { 0, 1, 1, 2 } };

        long total = 0;
        foreach (var p in passes)
        {
            int pw = (w - p[0] + p[2] - 1) / p[2], ph = (h - p[1] + p[3] - 1) / p[3];
            if (pw > 0 && ph > 0) total += (long)ph * (1 + ((long)pw * bitsPerPixel + 7) / 8);
        }

        idat.Position = 0;
        var raw = new byte[total];
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
        {
            int got = 0;
            while (got < raw.Length)
            {
                int n = z.Read(raw, got, raw.Length - got);
                if (n == 0) throw new FormatException("The PNG pixel data is cut short.");
                got += n;
            }
        }

        var img = new Image(w, h);
        int rawPos = 0;
        var line = new uint[w];
        foreach (var p in passes)
        {
            int pw = (w - p[0] + p[2] - 1) / p[2], ph = (h - p[1] + p[3] - 1) / p[3];
            if (pw <= 0 || ph <= 0) continue;
            int stride = (pw * bitsPerPixel + 7) / 8;
            byte[] prev = new byte[stride], cur = new byte[stride];
            for (int y = 0; y < ph; y++)
            {
                int filter = raw[rawPos++];
                if (filter > 4) throw new FormatException("The PNG uses an unknown row filter.");
                Array.Copy(raw, rawPos, cur, 0, stride);
                rawPos += stride;
                Unfilter(filter, cur, prev, bpp);
                ConvertRow(cur, pw, ctype, depth, plte, trns, line);
                int ty = p[1] + y * p[3];
                for (int x = 0; x < pw; x++) img.Pixels[ty * w + p[0] + x * p[2]] = line[x];
                (prev, cur) = (cur, prev);
            }
        }
        return img;
    }

    private static uint ReadBE(byte[] b, int at) => (uint)(b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3]);

    private static void Unfilter(int filter, byte[] cur, byte[] prev, int bpp)
    {
        for (int i = 0; i < cur.Length; i++)
        {
            int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
            int pred = filter switch { 0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2, _ => Paeth(a, b, c) };
            cur[i] = (byte)(cur[i] + pred);
        }
    }

    private static uint Pack(int r, int g, int b, int a) => (uint)(a << 24 | b << 16 | g << 8 | r);

    private static void ConvertRow(byte[] row, int width, int ctype, int depth, byte[]? plte, byte[]? trns, uint[] outp)
    {
        int max = (1 << Math.Min(depth, 8)) - 1;
        for (int x = 0; x < width; x++)
        {
            switch (ctype)
            {
                case 0:
                {
                    int v = Sample(row, x, depth), rawV = depth == 16 ? row[x * 2] << 8 | row[x * 2 + 1] : v;
                    int g = depth >= 8 ? v : v * 255 / max;
                    int a = trns is { Length: >= 2 } && rawV == (trns[0] << 8 | trns[1]) ? 0 : 255;
                    outp[x] = Pack(g, g, g, a);
                    break;
                }
                case 2:
                {
                    int s = depth == 16 ? 2 : 1, o = x * 3 * s;
                    int r = row[o], g = row[o + s], b = row[o + 2 * s];
                    bool clear = trns is { Length: >= 6 } && (depth == 16
                        ? (row[o] << 8 | row[o + 1]) == (trns[0] << 8 | trns[1]) && (row[o + 2] << 8 | row[o + 3]) == (trns[2] << 8 | trns[3]) && (row[o + 4] << 8 | row[o + 5]) == (trns[4] << 8 | trns[5])
                        : r == trns[1] && g == trns[3] && b == trns[5]);
                    outp[x] = Pack(r, g, b, clear ? 0 : 255);
                    break;
                }
                case 3:
                {
                    int i = Sample(row, x, depth);
                    if (i * 3 + 2 >= plte!.Length) throw new FormatException("The PNG uses a colour outside its palette.");
                    outp[x] = Pack(plte[i * 3], plte[i * 3 + 1], plte[i * 3 + 2], trns != null && i < trns.Length ? trns[i] : 255);
                    break;
                }
                case 4:
                {
                    int s = depth == 16 ? 2 : 1, o = x * 2 * s;
                    outp[x] = Pack(row[o], row[o], row[o], row[o + s]);
                    break;
                }
                default:
                {
                    int s = depth == 16 ? 2 : 1, o = x * 4 * s;
                    outp[x] = Pack(row[o], row[o + s], row[o + 2 * s], row[o + 3 * s]);
                    break;
                }
            }
        }
    }

    /// <summary>A 1-channel sample of 1/2/4/8/16 bits; 16-bit returns the high byte.</summary>
    private static int Sample(byte[] row, int x, int depth)
    {
        if (depth == 8) return row[x];
        if (depth == 16) return row[x * 2];
        int perByte = 8 / depth, shift = 8 - depth * (x % perByte + 1);
        return (row[x / perByte] >> shift) & ((1 << depth) - 1);
    }
}
