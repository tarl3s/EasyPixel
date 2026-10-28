using System.Text;
using EasyPixel.Core.Model;
using EasyPixel.Core.Raster;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Formats;

/// <summary>
/// GIF89a. Reading composites every frame onto the canvas (disposal methods 0-3, local or global palettes, a transparent
/// index, interlacing) into a one-layer RGBA sprite; a delay of 0 becomes 100 ms. Writing flattens each frame, gives it
/// its own palette (median cut, at most 255 colours plus one transparent entry), and loops forever.
/// </summary>
public sealed class GifFormat : ISpriteFormat
{
    private const long MaxPixels = 1L << 27;

    public string Name => "GIF animation";
    public string[] Extensions { get; } = { ".gif" };
    public bool CanRead => true;
    public bool CanWrite => true;

    // ---- reading ----

    private sealed class Cursor
    {
        private readonly byte[] _d;
        public int Pos;
        public Cursor(byte[] d) { _d = d; }
        public byte Byte() { if (Pos >= _d.Length) throw Cut(); return _d[Pos++]; }
        public int U16() { int a = Byte(); return a | Byte() << 8; }
        public byte[] Take(int n) { if (n < 0 || _d.Length - Pos < n) throw Cut(); var r = _d[Pos..(Pos + n)]; Pos += n; return r; }
        /// <summary>Joins the data sub-blocks up to the zero terminator.</summary>
        public byte[] SubBlocks()
        {
            var ms = new MemoryStream();
            for (int n = Byte(); n != 0; n = Byte()) ms.Write(Take(n));
            return ms.ToArray();
        }
    }

    private static FormatException Cut() => new("The GIF file is cut short.");

    public Sprite Read(Stream s, List<string> warnings)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        var c = new Cursor(ms.ToArray());
        try { return ReadCore(c); }
        catch (FormatException) { throw; }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException or OverflowException or InvalidOperationException or OutOfMemoryException)
        {
            throw new FormatException("The GIF data is damaged: " + e.Message, e);
        }
    }

    private static Sprite ReadCore(Cursor c)
    {
        var head = Encoding.ASCII.GetString(c.Take(6));
        if (head != "GIF87a" && head != "GIF89a") throw new FormatException("This is not a GIF file.");
        int w = c.U16(), h = c.U16();
        int flags = c.Byte();
        c.Byte(); c.Byte(); // background colour, aspect ratio
        if (w == 0 || h == 0 || (long)w * h > MaxPixels) throw new FormatException("The GIF image size is not supported.");
        Rgba[]? global = (flags & 0x80) != 0 ? ReadTable(c, 2 << (flags & 7)) : null;

        var sp = new Sprite(w, h);
        var layer = sp.Layers[0];
        var canvas = new uint[w * h];
        int frameCount = 0, disposal = 0, transparent = -1, delay = 0;
        while (true)
        {
            int b = c.Byte();
            if (b == 0x3B) break;
            if (b == 0x21)
            {
                int label = c.Byte();
                var data = c.SubBlocks();
                if (label == 0xF9 && data.Length >= 4)
                {
                    disposal = data[0] >> 2 & 7;
                    delay = data[1] | data[2] << 8;
                    transparent = (data[0] & 1) != 0 ? data[3] : -1;
                }
            }
            else if (b == 0x2C)
            {
                int left = c.U16(), top = c.U16(), fw = c.U16(), fh = c.U16();
                int f = c.Byte();
                if (fw == 0 || fh == 0 || (long)fw * fh > MaxPixels) throw new FormatException("A GIF frame has an invalid size.");
                var table = (f & 0x80) != 0 ? ReadTable(c, 2 << (f & 7)) : global ?? throw new FormatException("A GIF frame has no colour table.");
                int minCode = c.Byte();
                var idx = DecodeLzw(c.SubBlocks(), minCode, fw * fh);
                uint[]? saved = disposal == 3 ? (uint[])canvas.Clone() : null;

                for (int row = 0; row < fh; row++)
                {
                    int y = top + ((f & 0x40) != 0 ? InterlacedRow(row, fh) : row);
                    if ((uint)y >= (uint)h) continue;
                    for (int col = 0; col < fw; col++)
                    {
                        int x = left + col;
                        if ((uint)x >= (uint)w) continue;
                        int v = idx[row * fw + col];
                        if (v == transparent || v >= table.Length) continue;
                        canvas[y * w + x] = table[v].Packed;
                    }
                }

                var img = new Image(w, h);
                Array.Copy(canvas, img.Pixels, canvas.Length);
                if (frameCount > 0) sp.Frames.Add(new Frame());
                sp.Frames[frameCount].DurationMs = delay == 0 ? 100 : delay * 10;
                sp.Cels[(layer, frameCount)] = new Cel(img);
                frameCount++;

                if (disposal == 2)
                {
                    int x0 = Math.Max(0, left), x1 = Math.Min(w, left + fw);
                    for (int y = Math.Max(0, top); y < Math.Min(h, top + fh) && x1 > x0; y++)
                        Array.Clear(canvas, y * w + x0, x1 - x0);
                }
                else if (saved != null) canvas = saved;
                disposal = 0; transparent = -1; delay = 0;
            }
            else throw new FormatException("The GIF file has an unknown block.");
        }
        if (frameCount == 0) throw new FormatException("The GIF file has no images.");
        return sp;
    }

    private static Rgba[] ReadTable(Cursor c, int n)
    {
        var raw = c.Take(n * 3);
        var t = new Rgba[n];
        for (int i = 0; i < n; i++) t[i] = new Rgba(raw[i * 3], raw[i * 3 + 1], raw[i * 3 + 2], 255);
        return t;
    }

    /// <summary>Where the n-th stored row of an interlaced image really goes.</summary>
    private static int InterlacedRow(int n, int h)
    {
        int[] start = { 0, 4, 2, 1 }, step = { 8, 8, 4, 2 };
        for (int p = 0; p < 4; p++)
        {
            int count = Math.Max(0, (h - start[p] + step[p] - 1) / step[p]);
            if (n < count) return start[p] + n * step[p];
            n -= count;
        }
        return 0;
    }

    /// <summary>GIF-flavoured LZW. Stops when <paramref name="total"/> pixels are out; every loop step eats input, so corrupt data ends in an exception, never a hang.</summary>
    private static byte[] DecodeLzw(byte[] data, int minCode, int total)
    {
        if (minCode < 1 || minCode > 8) throw new FormatException("The GIF compression data is invalid.");
        int clear = 1 << minCode, eof = clear + 1;
        var prefix = new short[4096];
        var suffix = new byte[4096];
        var stack = new byte[4097];
        var output = new byte[total];
        int outPos = 0, next = eof + 1, size = minCode + 1, prev = -1;
        byte first = 0;
        long bitBuf = 0;
        int bitCount = 0, inPos = 0;

        while (outPos < total)
        {
            while (bitCount < size)
            {
                if (inPos >= data.Length) throw new FormatException("The GIF picture data is cut short.");
                bitBuf |= (long)data[inPos++] << bitCount;
                bitCount += 8;
            }
            int code = (int)(bitBuf & ((1 << size) - 1));
            bitBuf >>= size;
            bitCount -= size;

            if (code == clear) { size = minCode + 1; next = eof + 1; prev = -1; continue; }
            if (code == eof) break;

            if (prev < 0)
            {
                if (code >= clear) throw new FormatException("The GIF compression data is invalid.");
                output[outPos++] = (byte)code;
                prev = code; first = (byte)code;
                continue;
            }

            int cur = code, sp = 0;
            if (code > next || (code == next && next >= 4096)) throw new FormatException("The GIF compression data is invalid.");
            if (code == next) { stack[sp++] = first; cur = prev; }
            while (cur >= clear)
            {
                if (cur <= eof || sp >= stack.Length - 1) throw new FormatException("The GIF compression data is invalid.");
                stack[sp++] = suffix[cur];
                cur = prefix[cur];
            }
            first = (byte)cur;
            stack[sp++] = first;
            while (sp > 0 && outPos < total) output[outPos++] = stack[--sp];

            if (next < 4096)
            {
                prefix[next] = (short)prev;
                suffix[next] = first;
                next++;
                if (next == 1 << size && size < 12) size++;
            }
            prev = code;
        }
        if (outPos < total) throw new FormatException("The GIF picture data is cut short.");
        return output;
    }

    // ---- writing ----

    public void Write(Sprite sp, Stream s, List<string> warnings)
    {
        if (sp.Width > 65535 || sp.Height > 65535) throw new InvalidOperationException("GIF images are limited to 65535 pixels per side.");
        var o = new MemoryStream();
        void U16(int v) { o.WriteByte((byte)v); o.WriteByte((byte)(v >> 8)); }
        o.Write(Encoding.ASCII.GetBytes("GIF89a"));
        U16(sp.Width); U16(sp.Height);
        o.WriteByte(0); o.WriteByte(0); o.WriteByte(0); // no global table
        o.Write(new byte[] { 0x21, 0xFF, 0x0B });
        o.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        o.Write(new byte[] { 3, 1, 0, 0, 0 }); // loop forever

        var flat = new Image(sp.Width, sp.Height);
        var all = new PixelRect(0, 0, sp.Width, sp.Height);
        bool semi = false;
        for (int f = 0; f < sp.Frames.Count; f++)
        {
            Compositor.Flatten(sp, f, flat, all);
            var solid = new Image(sp.Width, sp.Height);
            for (int i = 0; i < flat.Pixels.Length; i++)
            {
                uint p = flat.Pixels[i];
                uint a = p >> 24;
                if (a != 0 && a != 255) semi = true;
                solid.Pixels[i] = a < 128 ? 0u : p | 0xFF000000u;
            }
            var pal = Quantize.MedianCut(solid, 255);
            var mapped = Quantize.ToPalette(solid, pal, false);
            var lookup = new Dictionary<uint, int>();
            for (int i = 0; i < pal.Colors.Count; i++) lookup.TryAdd(pal.Colors[i].Packed, i + 1);
            var indices = new byte[solid.Pixels.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = mapped.Pixels[i] == 0 ? (byte)0 : (byte)lookup[mapped.Pixels[i]];

            int bits = 1;
            while ((1 << bits) < pal.Colors.Count + 1) bits++;
            int delay = Math.Clamp((int)Math.Round(sp.Frames[f].DurationMs / 10.0), 2, 65535);
            o.Write(new byte[] { 0x21, 0xF9, 4, 2 << 2 | 1 });
            U16(delay);
            o.WriteByte(0); o.WriteByte(0);
            o.WriteByte(0x2C);
            U16(0); U16(0); U16(sp.Width); U16(sp.Height);
            o.WriteByte((byte)(0x80 | (bits - 1)));
            var table = new byte[3 << bits];
            for (int i = 0; i < pal.Colors.Count; i++)
            {
                table[(i + 1) * 3] = pal.Colors[i].R; table[(i + 1) * 3 + 1] = pal.Colors[i].G; table[(i + 1) * 3 + 2] = pal.Colors[i].B;
            }
            o.Write(table);
            int minCode = Math.Max(2, bits);
            o.WriteByte((byte)minCode);
            var lzw = EncodeLzw(indices, minCode);
            for (int i = 0; i < lzw.Length; i += 255)
            {
                int n = Math.Min(255, lzw.Length - i);
                o.WriteByte((byte)n);
                o.Write(lzw, i, n);
            }
            o.WriteByte(0);
        }
        o.WriteByte(0x3B);
        if (semi) warnings.Add("Semi-transparent pixels were made solid");
        s.Write(o.ToArray());
    }

    private static byte[] EncodeLzw(byte[] px, int minCode)
    {
        int clear = 1 << minCode, eof = clear + 1;
        var o = new MemoryStream();
        int buf = 0, count = 0, size = minCode + 1, next = eof + 1;
        var dict = new Dictionary<int, int>();
        void Emit(int code)
        {
            buf |= code << count;
            count += size;
            while (count >= 8) { o.WriteByte((byte)buf); buf >>= 8; count -= 8; }
        }
        Emit(clear);
        int prefix = px[0];
        for (int i = 1; i < px.Length; i++)
        {
            int key = prefix << 8 | px[i];
            if (dict.TryGetValue(key, out int hit)) { prefix = hit; continue; }
            Emit(prefix);
            if (next < 4096)
            {
                dict[key] = next;
                if (next == 1 << size && size < 12) size++;
                next++;
            }
            else
            {
                Emit(clear);
                dict.Clear();
                next = eof + 1;
                size = minCode + 1;
            }
            prefix = px[i];
        }
        Emit(prefix);
        Emit(eof);
        if (count > 0) o.WriteByte((byte)buf);
        return o.ToArray();
    }
}
