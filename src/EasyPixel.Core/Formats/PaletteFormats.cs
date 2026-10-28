using System.Globalization;
using System.Text;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>
/// Palette files: GIMP <c>.gpl</c>, plain <c>.hex</c> (one RRGGBB per line, as Lospec exports) and JASC <c>.pal</c>
/// (Paint Shop Pro, also read by Aseprite). They hold no alpha (a JASC line with a fourth value is read as alpha):
/// colours are written opaque.
/// Readers throw <see cref="FormatException"/> on bad input.
/// </summary>
public static class PaletteFormats
{
    public static readonly string[] Extensions = { ".gpl", ".hex", ".pal" };

    public static bool IsPaletteFile(string path) => Extensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    public static List<Rgba> Read(string path)
    {
        using var f = File.OpenRead(path);
        return Read(f, Path.GetExtension(path));
    }

    public static void Write(string path, IReadOnlyList<Rgba> colors, string name = "EasyPixel")
    {
        using var f = File.Create(path);
        Write(f, Path.GetExtension(path), colors, name);
    }

    /// <summary>Reads at most <see cref="Palette.MaxColors"/> colours from a file of the given extension (".gpl", ".hex", ".pal").</summary>
    public static List<Rgba> Read(Stream s, string extension)
    {
        using var r = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = r.ReadToEnd().Replace("\r", "").Split('\n');
        var colors = extension.ToLowerInvariant() switch
        {
            ".gpl" => ReadGpl(lines),
            ".hex" => ReadHex(lines),
            ".pal" => ReadPal(lines),
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
            default:
                throw new FormatException("Unknown palette file type: " + extension);
        }
        var bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
        s.Write(bytes);
    }

    private static string Inv(FormattableString f) => f.ToString(CultureInfo.InvariantCulture);

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

    private static byte Byte(string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v is >= 0 and <= 255
            ? (byte)v
            : throw new FormatException("Not a colour value (0-255): " + s);
}
