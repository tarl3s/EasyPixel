using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Formats;

/// <summary>
/// Palette files. Read and written: GIMP <c>.gpl</c>, plain <c>.hex</c> (one RRGGBB per line, as Lospec exports), JASC
/// <c>.pal</c> (Paint Shop Pro, also read by Aseprite), Paint.NET <c>.txt</c> (AARRGGBB), Adobe Color Table <c>.act</c>,
/// Photoshop Color Swatches <c>.aco</c> and Adobe Swatch Exchange <c>.ase</c>. Also read: Microsoft RIFF <c>.pal</c>
/// (told from JASC by its content), an Aseprite <c>.ase</c> (told from Adobe's by its content: the sprite's palette),
/// and any picture or sprite EasyPixel opens (its palette, or the colours it uses, in the order they first appear).
/// Colours are opaque unless the file says otherwise. Readers throw <see cref="FormatException"/> on bad input.
/// </summary>
public static class PaletteFormats
{
    /// <summary>Palette-only files, read and written.</summary>
    public static readonly string[] Extensions = { ".gpl", ".hex", ".pal", ".txt", ".act", ".aco", ".ase" };

    /// <summary>Everything a palette can be read from: palette files and every picture or sprite format.</summary>
    public static IEnumerable<string> ReadableExtensions =>
        Extensions.Concat(FormatRegistry.All.Where(f => f.CanRead).SelectMany(f => f.Extensions)).Distinct();

    public static List<Rgba> Read(string path) => Read(File.ReadAllBytes(path), Path.GetExtension(path));

    public static void Write(string path, IReadOnlyList<Rgba> colors, string name = "EasyPixel")
    {
        using var f = File.Create(path);
        Write(f, Path.GetExtension(path), colors, name);
    }

    public static List<Rgba> Read(Stream s, string extension)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        return Read(ms.ToArray(), extension);
    }

    /// <summary>Reads at most <see cref="Palette.MaxColors"/> colours from a file of the given extension.</summary>
    public static List<Rgba> Read(byte[] data, string extension)
    {
        var ext = extension.ToLowerInvariant();
        var colors = ext switch
        {
            ".gpl" => ReadGpl(Lines(data)),
            ".hex" => ReadHex(Lines(data)),
            ".pal" when Starts(data, "RIFF") => ReadRiffPal(data),
            ".pal" => ReadPal(Lines(data)),
            ".txt" => ReadPaintNet(Lines(data)),
            ".act" => ReadAct(data),
            ".aco" => ReadAco(data),
            ".ase" when Starts(data, "ASEF") => ReadAse(data),
            _ when FormatRegistry.All.FirstOrDefault(f => f.CanRead && f.Extensions.Contains(ext)) is { } format => FromSprite(format, data),
            _ => throw new FormatException("Unknown palette file type: " + extension),
        };
        if (colors.Count == 0) throw new FormatException("The palette file has no colours.");
        return colors.Count > Palette.MaxColors ? colors.GetRange(0, Palette.MaxColors) : colors;
    }

    public static void Write(Stream s, string extension, IReadOnlyList<Rgba> colors, string name = "EasyPixel")
    {
        var sb = new StringBuilder();
        switch (extension.ToLowerInvariant())
        {
            case ".gpl":
                sb.Append("GIMP Palette\n").Append("Name: ").Append(name).Append('\n').Append("Columns: 8\n#\n");
                foreach (var c in colors) sb.Append(Inv($"{c.R,3} {c.G,3} {c.B,3}\t#{c.R:X2}{c.G:X2}{c.B:X2}\n"));
                break;
            case ".hex":
                foreach (var c in colors) sb.Append(Inv($"{c.R:x2}{c.G:x2}{c.B:x2}\n"));
                break;
            case ".pal":
                sb.Append("JASC-PAL\n0100\n").Append(colors.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
                foreach (var c in colors) sb.Append(Inv($"{c.R} {c.G} {c.B}\n"));
                break;
            case ".txt":
                sb.Append("; paint.net Palette File\n; ").Append(name).Append('\n');
                foreach (var c in colors) sb.Append(Inv($"{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}\n"));
                break;
            case ".act": s.Write(WriteAct(colors)); return;
            case ".aco": s.Write(WriteAco(colors)); return;
            case ".ase": s.Write(WriteAse(colors)); return;
            default:
                throw new FormatException("Unknown palette file type: " + extension);
        }
        s.Write(new UTF8Encoding(false).GetBytes(sb.ToString()));
    }

    private static string Inv(FormattableString f) => f.ToString(CultureInfo.InvariantCulture);

    private static string[] Lines(byte[] data)
    {
        using var r = new StreamReader(new MemoryStream(data), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return r.ReadToEnd().Replace("\r", "").Split('\n');
    }

    private static bool Starts(byte[] d, string magic) => d.Length >= magic.Length && Encoding.ASCII.GetString(d, 0, magic.Length) == magic;

    // ---- text formats ----

    private static List<Rgba> ReadGpl(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "GIMP Palette") throw new FormatException("Not a GIMP palette (.gpl) file.");
        var colors = new List<Rgba>();
        foreach (var raw in lines.Skip(1))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("Name:") || line.StartsWith("Columns:")) continue;
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) throw new FormatException("A .gpl line is not \"R G B name\": " + line);
            colors.Add(new Rgba(Byte(parts[0]), Byte(parts[1]), Byte(parts[2]), 255));
        }
        return colors;
    }

    private static List<Rgba> ReadHex(string[] lines)
    {
        var colors = new List<Rgba>();
        foreach (var raw in lines)
        {
            var line = raw.Trim().TrimStart('#');
            if (line.Length == 0) continue;
            if (line.Length != 6 || !uint.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                throw new FormatException("A .hex line is not RRGGBB: " + raw.Trim());
            colors.Add(new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255));
        }
        return colors;
    }

    private static List<Rgba> ReadPal(string[] lines)
    {
        if (lines.Length < 3 || lines[0].Trim() != "JASC-PAL") throw new FormatException("Not a JASC palette (.pal) file.");
        if (!int.TryParse(lines[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 0)
            throw new FormatException("The .pal colour count is not a number.");
        var colors = new List<Rgba>();
        foreach (var raw in lines.Skip(3))
        {
            if (colors.Count == count) break;
            var parts = raw.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            if (parts.Length < 3) throw new FormatException("A .pal line is not \"R G B\": " + raw.Trim());
            colors.Add(new Rgba(Byte(parts[0]), Byte(parts[1]), Byte(parts[2]), parts.Length > 3 ? Byte(parts[3]) : (byte)255));
        }
        if (colors.Count < count) throw new FormatException("The .pal file ends before all its colours.");
        return colors;
    }

    /// <summary>Paint.NET: ";" comments, then AARRGGBB (or RRGGBB) per line.</summary>
    private static List<Rgba> ReadPaintNet(string[] lines)
    {
        var colors = new List<Rgba>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.Length is not (6 or 8) || !uint.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                throw new FormatException("A Paint.NET palette line is not AARRGGBB: " + line);
            colors.Add(new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v, line.Length == 8 ? (byte)(v >> 24) : (byte)255));
        }
        return colors;
    }

    private static byte Byte(string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v is >= 0 and <= 255
            ? (byte)v
            : throw new FormatException("Not a colour value (0-255): " + s);

    // ---- binary formats ----

    /// <summary>Microsoft RIFF palette: "RIFF" size "PAL " "data" size, version, count, then R G B flags per colour.</summary>
    private static List<Rgba> ReadRiffPal(byte[] d)
    {
        if (d.Length < 24 || Encoding.ASCII.GetString(d, 8, 4) != "PAL ") throw new FormatException("Not a RIFF palette (.pal) file.");
        for (int at = 12; at + 8 <= d.Length;)
        {
            var id = Encoding.ASCII.GetString(d, at, 4);
            int size = (int)BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(at + 4));
            if (id == "data")
            {
                if (at + 12 > d.Length) break;
                int count = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(at + 10));
                var colors = new List<Rgba>();
                for (int i = 0, p = at + 12; i < count && p + 4 <= d.Length; i++, p += 4) colors.Add(new Rgba(d[p], d[p + 1], d[p + 2], 255));
                return colors;
            }
            if (size < 0 || at + 8 + size > d.Length) break;
            at += 8 + size + (size & 1);
        }
        throw new FormatException("The RIFF palette has no colours.");
    }

    /// <summary>Adobe Color Table: 256 R G B triples, then (optionally) how many are used and the transparent one.</summary>
    private static List<Rgba> ReadAct(byte[] d)
    {
        if (d.Length < 768) throw new FormatException("Not an Adobe Color Table (.act) file.");
        int count = 256, transparent = -1;
        if (d.Length >= 772)
        {
            count = Math.Clamp((int)BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(768)), 1, 256);
            int t = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(770));
            if (t < 256) transparent = t;
        }
        var colors = new List<Rgba>();
        for (int i = 0; i < count; i++) colors.Add(new Rgba(d[i * 3], d[i * 3 + 1], d[i * 3 + 2], i == transparent ? (byte)0 : (byte)255));
        return colors;
    }

    private static byte[] WriteAct(IReadOnlyList<Rgba> colors)
    {
        var d = new byte[772];
        int n = Math.Min(colors.Count, 256);
        for (int i = 0; i < n; i++) { d[i * 3] = colors[i].R; d[i * 3 + 1] = colors[i].G; d[i * 3 + 2] = colors[i].B; }
        BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(768), (ushort)n);
        int t = colors.Take(n).ToList().FindIndex(c => c.A == 0);
        BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(770), t < 0 ? (ushort)0xFFFF : (ushort)t);
        return d;
    }

    /// <summary>Photoshop Color Swatches: version 1 (and 2, which repeats the colours with names), each colour a space and four 16-bit values.</summary>
    private static List<Rgba> ReadAco(byte[] d)
    {
        if (d.Length < 4 || BinaryPrimitives.ReadUInt16BigEndian(d) is not (1 or 2)) throw new FormatException("Not a Photoshop swatches (.aco) file.");
        bool v2 = BinaryPrimitives.ReadUInt16BigEndian(d) == 2;
        int count = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(2));
        var colors = new List<Rgba>();
        int p = 4;
        for (int i = 0; i < count; i++)
        {
            if (p + 10 > d.Length) throw new FormatException("The .aco file ends before all its colours.");
            int space = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p));
            double w = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p + 2)), x = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p + 4)),
                   y = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p + 6)), z = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p + 8));
            p += 10;
            if (v2 && p + 4 <= d.Length) p += 4 + 2 * (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(p)); // name: length + UTF-16 chars
            var c = space switch
            {
                0 => Rgb(w / 65535, x / 65535, y / 65535),
                1 => Hsb(w / 65535 * 360, x / 65535, y / 65535),
                2 => Cmyk(1 - w / 65535, 1 - x / 65535, 1 - y / 65535, 1 - z / 65535), // 0 = full ink
                7 => Lab(w / 100, (short)x / 100.0, (short)y / 100.0),
                8 => Rgb(1 - w / 10000, 1 - w / 10000, 1 - w / 10000),
                _ => (Rgba?)null, // spot colour systems (Pantone…) have no RGB in the file
            };
            if (c is { } col) colors.Add(col);
        }
        return colors;
    }

    private static byte[] WriteAco(IReadOnlyList<Rgba> colors)
    {
        var d = new byte[4 + colors.Count * 10];
        BinaryPrimitives.WriteUInt16BigEndian(d, 1);
        BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(2), (ushort)colors.Count);
        for (int i = 0; i < colors.Count; i++)
        {
            var s = d.AsSpan(4 + i * 10);
            BinaryPrimitives.WriteUInt16BigEndian(s[2..], (ushort)(colors[i].R * 257));
            BinaryPrimitives.WriteUInt16BigEndian(s[4..], (ushort)(colors[i].G * 257));
            BinaryPrimitives.WriteUInt16BigEndian(s[6..], (ushort)(colors[i].B * 257));
        }
        return d;
    }

    /// <summary>Adobe Swatch Exchange: "ASEF", version, blocks (colour entries, group starts and ends), big-endian.</summary>
    private static List<Rgba> ReadAse(byte[] d)
    {
        if (d.Length < 12) throw new FormatException("Not an Adobe swatch exchange (.ase) file.");
        int blocks = (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(8));
        var colors = new List<Rgba>();
        int p = 12;
        for (int b = 0; b < blocks && p + 6 <= d.Length; b++)
        {
            int type = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(p));
            int len = (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(p + 2));
            int body = p + 6;
            p = body + len;
            if (len < 0 || p > d.Length) throw new FormatException("The .ase file ends inside a block.");
            if (type != 0x0001) continue; // group start / end
            int at = body + 2 + 2 * BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(body)); // name: length (UTF-16 chars) + chars
            if (at + 4 > p) continue;
            var model = Encoding.ASCII.GetString(d, at, 4);
            float F(int i) => BinaryPrimitives.ReadSingleBigEndian(d.AsSpan(at + 4 + i * 4));
            var c = model switch
            {
                "RGB " => Rgb(F(0), F(1), F(2)),
                "CMYK" => Cmyk(F(0), F(1), F(2), F(3)),
                "Gray" => Rgb(F(0), F(0), F(0)),
                "LAB " => Lab(F(0), F(1), F(2)),
                _ => (Rgba?)null,
            };
            if (c is { } col) colors.Add(col);
        }
        return colors;
    }

    private static byte[] WriteAse(IReadOnlyList<Rgba> colors)
    {
        var ms = new MemoryStream();
        void U16(int v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, (ushort)v); ms.Write(b); }
        void U32(int v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, (uint)v); ms.Write(b); }
        void F32(float v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteSingleBigEndian(b, v); ms.Write(b); }
        ms.Write(Encoding.ASCII.GetBytes("ASEF"));
        U16(1); U16(0); U32(colors.Count);
        foreach (var c in colors)
        {
            var name = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            U16(0x0001);
            U32(2 + (name.Length + 1) * 2 + 4 + 12 + 2);
            U16(name.Length + 1);
            ms.Write(Encoding.BigEndianUnicode.GetBytes(name)); U16(0);
            ms.Write(Encoding.ASCII.GetBytes("RGB "));
            F32(c.R / 255f); F32(c.G / 255f); F32(c.B / 255f);
            U16(2); // normal (not global / spot)
        }
        return ms.ToArray();
    }

    // ---- pictures and sprites ----

    /// <summary>A picture's palette: an indexed sprite's own palette, else the colours it uses in the order they first appear.</summary>
    private static List<Rgba> FromSprite(ISpriteFormat format, byte[] data)
    {
        Sprite s;
        try { s = format.Read(new MemoryStream(data), new List<string>()); }
        catch (Exception ex) when (ex is not FormatException) { throw new FormatException(ex.Message); }
        if (s.Palette.Colors.Count > 0 && s.Mode == ColorMode.Indexed)
            return s.Palette.Colors.Where(c => c.A > 0).ToList(); // the transparent entry isn't a colour to paint with
        var colors = new List<Rgba>();
        var seen = new HashSet<uint>();
        var flat = new Image(s.Width, s.Height);
        for (int f = 0; f < s.Frames.Count && colors.Count < Palette.MaxColors; f++)
        {
            Compositor.Flatten(s, f, flat, new PixelRect(0, 0, s.Width, s.Height));
            foreach (var px in flat.Pixels)
            {
                var c = Rgba.FromPacked(px);
                if (c.A == 0 || !seen.Add(c.Packed)) continue;
                colors.Add(c);
                if (colors.Count == Palette.MaxColors) break;
            }
        }
        if (colors.Count == 0) return s.Palette.Colors.Where(c => c.A > 0).ToList(); // a blank sprite that brings a palette
        return colors;
    }

    // ---- colour models ----

    private static byte B(double v) => (byte)Math.Clamp((int)Math.Round(v * 255), 0, 255);
    private static Rgba Rgb(double r, double g, double b) => new(B(r), B(g), B(b), 255);
    private static Rgba Cmyk(double c, double m, double y, double k) => Rgb((1 - c) * (1 - k), (1 - m) * (1 - k), (1 - y) * (1 - k));

    private static Rgba Hsb(double h, double s, double v)
    {
        double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
        var (r, g, b) = (h % 360) switch
        {
            < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x),
            < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x),
        };
        return Rgb(r + m, g + m, b + m);
    }

    /// <summary>CIE L*a*b* (D50, as Adobe stores it; L 0–100) to sRGB.</summary>
    private static Rgba Lab(double l, double a, double bb)
    {
        if (l <= 1) l *= 100; // ASE stores L as 0–1, ACO as 0–100
        double fy = (l + 16) / 116;
        double fx = fy + a / 500, fz = fy - bb / 200;
        double F(double t) => t * t * t > 0.008856 ? t * t * t : (t - 16.0 / 116) / 7.787;
        double x = 0.9642 * F(fx), y = F(fy), z = 0.8249 * F(fz);
        // D50 XYZ → linear sRGB (Bradford-adapted)
        double r = 3.1338561 * x - 1.6168667 * y - 0.4906146 * z;
        double g = -0.9787684 * x + 1.9161415 * y + 0.0334540 * z;
        double b = 0.0719453 * x - 0.2289914 * y + 1.4052427 * z;
        double G(double v) => v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(Math.Max(v, 0), 1 / 2.4) - 0.055;
        return Rgb(G(r), G(g), G(b));
    }
}
