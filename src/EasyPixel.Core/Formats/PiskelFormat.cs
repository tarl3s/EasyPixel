using System.Text;
using System.Text.Json;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>
/// Piskel (.piskel): JSON with <c>modelVersion</c> 2, <c>piskel.fps</c> and layers that are themselves JSON strings holding
/// base64 PNG chunks plus a <c>layout</c> (layout[column][row] = frame index placed at column*width, row*height).
/// The older single-strip form (a layer with <c>base64PNG</c> and frames side by side) is readable too.
/// Piskel has one speed, no groups, no hidden layers and no blend modes; whatever does not fit is reported as a warning.
/// </summary>
public sealed class PiskelFormat : ISpriteFormat
{
    private const long MaxPixels = 1L << 28;
    private const int MaxFrames = 10000;

    public string Name => "Piskel";
    public string[] Extensions { get; } = { ".piskel" };
    public bool CanRead => true;
    public bool CanWrite => true;

    // ---- writing ----

    public void Write(Sprite sp, Stream s, List<string> warnings)
    {
        var layers = sp.Layers.Where(l => !l.IsGroup).ToList();
        if (sp.Layers.Any(l => l.IsGroup)) warnings.Add("Layer groups were flattened");
        if (layers.Any(l => !l.Visible)) warnings.Add("Hidden layers were not saved");
        layers = layers.Where(l => l.Visible).ToList();
        if (layers.Any(l => l.Blend != BlendMode.Normal)) warnings.Add("Blend modes are not supported by Piskel and were ignored");

        int n = sp.Frames.Count;
        double avg = sp.Frames.Average(f => Math.Max(1, f.DurationMs));
        if (sp.Frames.Any(f => f.DurationMs != sp.Frames[0].DurationMs)) warnings.Add("Frame durations were averaged (Piskel stores one speed)");
        int fps = Math.Max(1, (int)Math.Round(1000.0 / avg));

        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteNumber("modelVersion", 2);
            w.WriteStartObject("piskel");
            w.WriteString("name", "EasyPixel");
            w.WriteString("description", "");
            w.WriteNumber("fps", fps);
            w.WriteNumber("height", sp.Height);
            w.WriteNumber("width", sp.Width);
            w.WriteStartArray("layers");
            if (layers.Count == 0) w.WriteStringValue(LayerJson(sp, null, n));
            foreach (var l in layers) w.WriteStringValue(LayerJson(sp, l, n));
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndObject();
        }
        s.Write(ms.ToArray());
    }

    private static string LayerJson(Sprite sp, Layer? layer, int frames)
    {
        var strip = new Image(sp.Width * frames, sp.Height);
        if (layer != null)
            for (int f = 0; f < frames; f++)
            {
                if (!sp.Cels.TryGetValue((layer, f), out var cel)) continue;
                float op = Math.Clamp(cel.Opacity, 0f, 1f);
                for (int y = 0; y < cel.Image.Height; y++)
                    for (int x = 0; x < cel.Image.Width; x++)
                    {
                        int dx = cel.X + x, dy = cel.Y + y;
                        if ((uint)dx >= (uint)sp.Width || (uint)dy >= (uint)sp.Height) continue;
                        uint p = cel.Image.Pixels[y * cel.Image.Width + x];
                        p = sp.ToPacked(p);
                        if (op < 1f) p = (p & 0x00FFFFFF) | (uint)MathF.Round((p >> 24) * op) << 24;
                        strip.Pixels[dy * strip.Width + f * sp.Width + dx] = p;
                    }
            }

        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteString("name", layer?.Name ?? "Layer 1");
            w.WriteNumber("opacity", (layer?.Opacity ?? 255) / 255.0);
            w.WriteNumber("frameCount", frames);
            w.WriteStartArray("chunks");
            w.WriteStartObject();
            w.WriteStartArray("layout");
            for (int f = 0; f < frames; f++) { w.WriteStartArray(); w.WriteNumberValue(f); w.WriteEndArray(); }
            w.WriteEndArray();
            w.WriteString("base64PNG", "data:image/png;base64," + Convert.ToBase64String(Png.Encode(strip)));
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ---- reading ----

    public Sprite Read(Stream s, List<string> warnings)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        try { return ReadCore(ms.ToArray()); }
        catch (FormatException) { throw; }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or OverflowException
                                      or ArgumentException or IndexOutOfRangeException or OutOfMemoryException)
        {
            throw new FormatException("The Piskel file is damaged: " + e.Message, e);
        }
    }

    private static Sprite ReadCore(byte[] data)
    {
        using var doc = JsonDocument.Parse(data);
        if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("piskel", out var p) || p.ValueKind != JsonValueKind.Object)
            throw new FormatException("This is not a Piskel file.");
        int w = Int(p, "width", 0), h = Int(p, "height", 0);
        double fps = p.TryGetProperty("fps", out var fe) && fe.ValueKind == JsonValueKind.Number ? fe.GetDouble() : 12;
        int duration = fps > 0 ? Math.Max(1, (int)Math.Round(1000 / fps)) : 100;
        if (w <= 0 || h <= 0) throw new FormatException("The Piskel file has no valid size.");
        if (!p.TryGetProperty("layers", out var layersEl) || layersEl.ValueKind != JsonValueKind.Array || layersEl.GetArrayLength() == 0)
            throw new FormatException("The Piskel file has no layers.");

        var parsed = new List<(string name, double opacity, int frames, List<(Image png, int[][] layout)> chunks)>();
        foreach (var le in layersEl.EnumerateArray())
        {
            JsonDocument? owned = le.ValueKind == JsonValueKind.String ? JsonDocument.Parse(le.GetString()!) : null;
            using (owned)
            {
                var l = owned?.RootElement ?? le;
                if (l.ValueKind != JsonValueKind.Object) throw new FormatException("A Piskel layer is invalid.");
                string name = l.TryGetProperty("name", out var ne) && ne.ValueKind == JsonValueKind.String ? ne.GetString()! : "Layer";
                double opacity = l.TryGetProperty("opacity", out var oe) && oe.ValueKind == JsonValueKind.Number ? oe.GetDouble() : 1;
                int frames = Int(l, "frameCount", 0);
                var chunks = new List<(Image, int[][])>();
                if (l.TryGetProperty("chunks", out var ce) && ce.ValueKind == JsonValueKind.Array)
                {
                    foreach (var chunk in ce.EnumerateArray())
                    {
                        var layout = chunk.GetProperty("layout").EnumerateArray()
                            .Select(col => col.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
                        chunks.Add((DecodePng(chunk.GetProperty("base64PNG").GetString()), layout));
                    }
                }
                else if (l.TryGetProperty("base64PNG", out var be))
                {
                    var png = DecodePng(be.GetString());
                    if (frames <= 0) frames = Math.Max(1, png.Width / w);
                    chunks.Add((png, Enumerable.Range(0, frames).Select(i => new[] { i }).ToArray()));
                }
                else throw new FormatException("A Piskel layer has no pictures.");
                if (frames <= 0) frames = chunks.SelectMany(c => c.Item2).SelectMany(x => x).DefaultIfEmpty(-1).Max() + 1;
                parsed.Add((name, opacity, frames, chunks));
            }
        }

        int count = parsed.Max(l => l.frames);
        if (count < 1 || count > MaxFrames || (long)w * h * count * parsed.Count > MaxPixels)
            throw new FormatException("The Piskel file is too large or empty.");

        var sp = new Sprite(w, h);
        for (int f = 0; f < count; f++)
        {
            if (f > 0) sp.Frames.Add(new Frame());
            sp.Frames[f].DurationMs = duration;
        }
        for (int li = 0; li < parsed.Count; li++)
        {
            var (name, opacity, frames, chunks) = parsed[li];
            var layer = li == 0 ? sp.Layers[0] : new Layer();
            if (li > 0) sp.Layers.Add(layer);
            layer.Name = name;
            layer.Opacity = (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255);
            for (int f = 0; f < count; f++) sp.Cels[(layer, f)] = new Cel(new Image(w, h));
            foreach (var (png, layout) in chunks)
                for (int col = 0; col < layout.Length; col++)
                    for (int row = 0; row < layout[col].Length; row++)
                    {
                        int f = layout[col][row];
                        if (f < 0 || f >= count) throw new FormatException("A Piskel layout points at a missing frame.");
                        if ((col + 1) * w > png.Width || (row + 1) * h > png.Height) throw new FormatException("A Piskel picture is smaller than its layout.");
                        var dest = sp.Cels[(layer, f)].Image;
                        for (int y = 0; y < h; y++)
                            Array.Copy(png.Pixels, (row * h + y) * png.Width + col * w, dest.Pixels, y * w, w);
                    }
        }
        return sp;
    }

    private static Image DecodePng(string? dataUri)
    {
        if (dataUri == null) throw new FormatException("A Piskel picture is missing.");
        int comma = dataUri.IndexOf(',');
        return Png.Decode(Convert.FromBase64String(comma >= 0 ? dataUri[(comma + 1)..] : dataUri));
    }

    private static int Int(JsonElement e, string name, int fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) ? i : fallback;
}
