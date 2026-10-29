using System.IO.Compression;
using System.Text.Json;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>
/// EasyPixel's own format: a zip with <c>sprite.json</c> (every model field, <c>"format":1</c>) and one PNG per cel in
/// <c>cels/&lt;layer&gt;_&lt;frame&gt;.png</c>. A linked cel (one cel shown in several frames of a layer) is written once; the other frames
/// say <c>"linkTo": &lt;first frame&gt;</c> instead of naming a picture (optional key, still format 1). Layers (and a layer's parent) are referenced by their index in the layer list.
/// RGBA cels are stored as 8-bit RGBA PNGs; indexed cels as 8-bit greyscale PNGs whose value is the palette index
/// (<see cref="Png.EncodeGrey8"/>), so indices stay lossless. Unknown keys in the JSON are ignored; unknown chunks are kept as base64.
/// </summary>
public sealed class EpxFormat : ISpriteFormat
{
    public const int Version = 1;
    public const string UnknownValueWarning = "Some settings had values this version doesn't know; defaults were used";
    private const long MaxEntryBytes = 512L * 1024 * 1024;

    public string Name => "EasyPixel sprite";
    public string[] Extensions { get; } = { ".epx" };
    public bool CanRead => true;
    public bool CanWrite => true;

    public void Write(Sprite sp, Stream s, List<string> warnings)
    {
        using var zip = new ZipArchive(s, ZipArchiveMode.Create, leaveOpen: true);
        var celList = sp.Cels
            .Select(kv => (layer: sp.Layers.IndexOf(kv.Key.Item1), frame: kv.Key.Item2, cel: kv.Value))
            .Where(c => c.layer >= 0)
            .OrderBy(c => c.layer).ThenBy(c => c.frame).ToList();
        // A cel object stored under several frames of a layer is linked: it is written once, at its first frame.
        var firstFrame = new Dictionary<(int Layer, Cel Cel), int>();
        foreach (var (layer, frame, cel) in celList) firstFrame.TryAdd((layer, cel), frame);
        bool IsLink(int layer, int frame, Cel cel) => firstFrame[(layer, cel)] != frame;

        using (var es = zip.CreateEntry("sprite.json").Open())
        using (var w = new Utf8JsonWriter(es, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteNumber("format", Version);
            w.WriteNumber("width", sp.Width);
            w.WriteNumber("height", sp.Height);
            w.WriteString("mode", sp.Mode == ColorMode.Indexed ? "indexed" : "rgba");
            w.WriteStartObject("palette");
            w.WriteNumber("transparentIndex", sp.Palette.TransparentIndex);
            w.WriteStartArray("colors");
            foreach (var c in sp.Palette.Colors) w.WriteStringValue(c.Packed.ToString("X8"));
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteStartArray("layers");
            foreach (var l in sp.Layers)
            {
                w.WriteStartObject();
                w.WriteString("name", l.Name);
                w.WriteBoolean("visible", l.Visible);
                w.WriteBoolean("locked", l.Locked);
                w.WriteNumber("opacity", l.Opacity);
                w.WriteString("blend", l.Blend.ToString());
                w.WriteNumber("parent", l.Parent == null ? -1 : sp.Layers.IndexOf(l.Parent));
                w.WriteBoolean("isGroup", l.IsGroup);
                WriteExtra(w, l.Extra);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartArray("frames");
            foreach (var f in sp.Frames) { w.WriteStartObject(); w.WriteNumber("durationMs", f.DurationMs); WriteExtra(w, f.Extra); w.WriteEndObject(); }
            w.WriteEndArray();
            w.WriteStartArray("tags");
            foreach (var t in sp.Tags)
            {
                w.WriteStartObject();
                w.WriteString("name", t.Name);
                w.WriteNumber("from", t.From);
                w.WriteNumber("to", t.To);
                w.WriteString("dir", t.Dir.ToString());
                w.WriteString("color", t.Color.Packed.ToString("X8"));
                WriteExtra(w, t.Extra);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartArray("cels");
            foreach (var (layer, frame, cel) in celList)
            {
                w.WriteStartObject();
                w.WriteNumber("layer", layer);
                w.WriteNumber("frame", frame);
                if (IsLink(layer, frame, cel))
                {
                    w.WriteNumber("linkTo", firstFrame[(layer, cel)]);
                    w.WriteEndObject();
                    continue;
                }
                w.WriteNumber("x", cel.X);
                w.WriteNumber("y", cel.Y);
                w.WriteNumber("opacity", cel.Opacity);
                w.WriteString("file", CelFile(layer, frame));
                WriteExtra(w, cel.Extra);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartObject("unknownChunks");
            foreach (var (k, v) in sp.UnknownChunks) w.WriteString(k, Convert.ToBase64String(v));
            w.WriteEndObject();
            w.WriteEndObject();
        }

        foreach (var (layer, frame, cel) in celList)
        {
            if (IsLink(layer, frame, cel)) continue;
            byte[] png = sp.Mode == ColorMode.Indexed ? Png.EncodeGrey8(cel.Image) : Png.Encode(cel.Image);
            using var es = zip.CreateEntry(CelFile(layer, frame)).Open();
            es.Write(png);
        }
    }

    private static string CelFile(int layer, int frame) => $"cels/{layer}_{frame}.png";

    public Sprite Read(Stream s, List<string> warnings)
    {
        try { return ReadCore(s, warnings); }
        catch (FormatException) { throw; }
        catch (Exception e) when (e is InvalidDataException or JsonException or InvalidOperationException or KeyNotFoundException
                                      or ArgumentException or OverflowException or EndOfStreamException or IOException or IndexOutOfRangeException)
        {
            throw new FormatException("This file is damaged and could not be read: " + e.Message, e);
        }
    }

    private static Sprite ReadCore(Stream input, List<string> warnings)
    {
        var buf = new MemoryStream();
        input.CopyTo(buf);
        buf.Position = 0;
        using var zip = new ZipArchive(buf, ZipArchiveMode.Read);
        var jsonBytes = ReadEntry(zip, "sprite.json") ?? throw new FormatException("This is not an EasyPixel file (sprite.json is missing).");
        using var doc = JsonDocument.Parse(jsonBytes);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new FormatException("The sprite description is not valid.");

        int version = root.GetProperty("format").GetInt32();
        if (version > Version) throw new FormatException("This file was made by a newer EasyPixel. Update EasyPixel to open it.");

        var mode = root.GetProperty("mode").GetString() == "indexed" ? ColorMode.Indexed : ColorMode.Rgba;
        var sp = new Sprite(root.GetProperty("width").GetInt32(), root.GetProperty("height").GetInt32(), mode);
        // The constructor adds a default layer, frame and (indexed) transparent entry; the file defines everything itself.
        sp.Layers.Clear();
        sp.Frames.Clear();
        sp.Palette = new Palette();

        var pal = root.GetProperty("palette");
        sp.Palette.TransparentIndex = pal.GetProperty("transparentIndex").GetInt32();
        foreach (var c in pal.GetProperty("colors").EnumerateArray())
            sp.Palette.Colors.Add(Rgba.FromPacked(Convert.ToUInt32(c.GetString(), 16)));
        if (sp.Palette.Colors.Count > Palette.MaxColors) throw new FormatException("The palette has too many colours.");

        var parents = new List<int>();
        foreach (var l in root.GetProperty("layers").EnumerateArray())
        {
            sp.Layers.Add(new Layer
            {
                Name = l.GetProperty("name").GetString() ?? "",
                Visible = l.GetProperty("visible").GetBoolean(),
                Locked = l.GetProperty("locked").GetBoolean(),
                Opacity = l.GetProperty("opacity").GetByte(),
                Blend = ReadEnum(l.GetProperty("blend").GetString(), BlendMode.Normal, warnings),
                IsGroup = l.GetProperty("isGroup").GetBoolean(),
            });
            parents.Add(l.GetProperty("parent").GetInt32());
            ReadExtra(l, sp.Layers[^1].Extra);
        }
        for (int i = 0; i < parents.Count; i++)
            if (parents[i] != -1)
            {
                if (parents[i] < 0 || parents[i] >= parents.Count || !sp.Layers[parents[i]].IsGroup) throw new FormatException("A layer points at a missing group.");
                sp.Layers[i].Parent = sp.Layers[parents[i]];
            }
        for (int i = 0; i < parents.Count; i++) // a group inside itself would loop forever when drawing
            for (int p = parents[i], steps = 0; p != -1; p = parents[p])
                if (++steps > parents.Count) throw new FormatException("Layer groups contain themselves.");

        foreach (var f in root.GetProperty("frames").EnumerateArray())
        {
            sp.Frames.Add(new Frame { DurationMs = f.GetProperty("durationMs").GetInt32() });
            ReadExtra(f, sp.Frames[^1].Extra);
        }
        if (sp.Layers.Count == 0 || sp.Frames.Count == 0) throw new FormatException("The sprite has no layers or no frames.");

        foreach (var t in root.GetProperty("tags").EnumerateArray())
        {
            sp.Tags.Add(new Tag
            {
                Name = t.GetProperty("name").GetString() ?? "",
                From = Math.Clamp(t.GetProperty("from").GetInt32(), 0, sp.Frames.Count - 1),
                To = Math.Clamp(t.GetProperty("to").GetInt32(), 0, sp.Frames.Count - 1),
                Dir = ReadEnum(t.GetProperty("dir").GetString(), LoopDir.Forward, warnings),
                Color = Rgba.FromPacked(Convert.ToUInt32(t.GetProperty("color").GetString(), 16)),
            });
            if (sp.Tags[^1].From > sp.Tags[^1].To) (sp.Tags[^1].From, sp.Tags[^1].To) = (sp.Tags[^1].To, sp.Tags[^1].From);
            ReadExtra(t, sp.Tags[^1].Extra);
        }

        var links = new List<(Layer Layer, int Frame, int To)>();
        foreach (var c in root.GetProperty("cels").EnumerateArray())
        {
            int li = c.GetProperty("layer").GetInt32(), fi = c.GetProperty("frame").GetInt32();
            if (li < 0 || li >= sp.Layers.Count || fi < 0 || fi >= sp.Frames.Count) throw new FormatException("A cel points at a missing layer or frame.");
            if (c.TryGetProperty("linkTo", out var link)) { links.Add((sp.Layers[li], fi, link.GetInt32())); continue; }
            var png = ReadEntry(zip, c.GetProperty("file").GetString() ?? "") ?? throw new FormatException("A cel picture is missing from the file.");
            var image = mode == ColorMode.Indexed ? Png.DecodeGrey8(png) : Png.Decode(png);
            sp.Cels[(sp.Layers[li], fi)] = new Cel(image, c.GetProperty("x").GetInt32(), c.GetProperty("y").GetInt32())
            {
                Opacity = c.GetProperty("opacity").GetSingle(),
            };
            ReadExtra(c, sp.Cels[(sp.Layers[li], fi)].Extra);
        }
        foreach (var (layer, frame, to) in links) // linked cels share the cel object of the frame they point at
            sp.Cels[(layer, frame)] = sp.Cels.TryGetValue((layer, to), out var shared) ? shared
                : throw new FormatException("A linked cel points at a missing cel.");

        if (mode == ColorMode.Indexed) // an indexed sprite always has a transparent entry (the eraser and holes use it)
        {
            if (sp.Palette.TransparentIndex >= sp.Palette.Colors.Count) sp.Palette.TransparentIndex = -1;
            if (sp.Palette.TransparentIndex < 0) sp.EnsureTransparentIndex();
        }

        if (root.TryGetProperty("unknownChunks", out var uc) && uc.ValueKind == JsonValueKind.Object)
            foreach (var p in uc.EnumerateObject()) sp.UnknownChunks[p.Name] = Convert.FromBase64String(p.Value.GetString() ?? "");
        return sp;
    }

    /// <summary>A named enum value; an unknown name or a number (which <c>Enum.Parse</c> would accept) falls back with a warning.</summary>
    private static T ReadEnum<T>(string? name, T fallback, List<string> warnings) where T : struct, Enum
    {
        if (name != null && Enum.GetNames<T>().Contains(name)) return Enum.Parse<T>(name);
        if (!warnings.Contains(UnknownValueWarning)) warnings.Add(UnknownValueWarning);
        return fallback;
    }

    /// <summary>Format-specific data of a layer, frame, tag or cel, as base64 under "extra" (left out when empty).</summary>
    private static void WriteExtra(Utf8JsonWriter w, Dictionary<string, byte[]> extra)
    {
        if (extra.Count == 0) return;
        w.WriteStartObject("extra");
        foreach (var (k, v) in extra) w.WriteString(k, Convert.ToBase64String(v));
        w.WriteEndObject();
    }

    private static void ReadExtra(JsonElement e, Dictionary<string, byte[]> extra)
    {
        if (e.TryGetProperty("extra", out var x) && x.ValueKind == JsonValueKind.Object)
            foreach (var p in x.EnumerateObject()) extra[p.Name] = Convert.FromBase64String(p.Value.GetString() ?? "");
    }

    private static byte[]? ReadEntry(ZipArchive zip, string name)
    {
        var e = zip.GetEntry(name);
        if (e == null) return null;
        if (e.Length > MaxEntryBytes) throw new FormatException("A part of the file is too large.");
        var ms = new MemoryStream();
        using (var es = e.Open()) es.CopyTo(ms);
        return ms.ToArray();
    }
}
