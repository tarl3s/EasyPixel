using System.Globalization;
using System.Text.Json;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Bridge;

/// <summary>An export waiting for EasyEngine: its folder, name, speed, frame count and when it was written.</summary>
public sealed record HandoffInfo(string Folder, string Name, int Fps, int FrameCount, DateTime When);

/// <summary>
/// The handoff folder EasyEngine imports from: <c>%LOCALAPPDATA%\EasyPixel\Exports\&lt;name&gt;-&lt;yyyyMMdd-HHmmss&gt;\</c> with
/// <c>frame_000.png…</c> and <c>easypixel-export.json</c>
/// (<c>{"format":1,"name","fps","frames":[…],"width","height","source"}</c>). The reader half (<see cref="ListRecent"/>)
/// is shared source: EasyEngine carries a copy, so keep it free of the rest of the model.
/// </summary>
public static class ExportHandoff
{
    public const string ManifestName = "easypixel-export.json";
    public const int Format = 1;

    /// <summary>Where exports go unless a caller says otherwise.</summary>
    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyPixel", "Exports");

    /// <summary>1000 / the average frame time in ms, rounded; 10 when the times are unusable.</summary>
    public static int FpsFor(IReadOnlyCollection<int> durationsMs)
    {
        if (durationsMs.Count == 0) return 10;
        double avg = durationsMs.Average();
        return avg <= 0 ? 10 : Math.Max(1, (int)Math.Round(1000.0 / avg, MidpointRounding.AwayFromZero));
    }

    /// <summary>Writes every tag as its own export, or the whole timeline when the sprite has no tags. Returns the folders.</summary>
    public static IReadOnlyList<string> WriteAll(Sprite s, string name, string? root = null, string? source = null, DateTime? when = null)
    {
        if (s.Tags.Count == 0) return new[] { WriteFrames(s, null, root, name, source, when) };
        // Each tag object, not its name: two tags with the same name are two exports (the folder gets a "-2").
        return s.Tags.Select(t => WriteFrames(s, t, root, t.Name, source, when)).ToList();
    }

    /// <summary>
    /// Writes the frames of tag <paramref name="tag"/> (the whole timeline when null) flattened to PNGs plus the manifest;
    /// returns the new folder. <paramref name="name"/> defaults to the tag, then "sprite".
    /// </summary>
    public static string Write(Sprite s, string? tag, string? root = null, string? name = null, string? source = null, DateTime? when = null)
    {
        var t = tag == null ? null : s.Tags.FirstOrDefault(x => x.Name == tag) ?? throw new ArgumentException("No such tag: " + tag, nameof(tag));
        return WriteFrames(s, t, root, name, source, when);
    }

    /// <summary>
    /// The frames a tag plays, in order: Forward from..to, Reverse to..from, ping-pong from..to and back without
    /// repeating either end (the whole timeline when <paramref name="t"/> is null).
    /// </summary>
    public static List<int> FrameOrder(Sprite s, Tag? t)
    {
        int last = s.Frames.Count - 1;
        int from = t == null ? 0 : Math.Clamp(Math.Min(t.From, t.To), 0, last);
        int to = t == null ? last : Math.Clamp(Math.Max(t.From, t.To), 0, last);
        var order = Enumerable.Range(from, to - from + 1).ToList();
        if (t?.Dir == LoopDir.Reverse) order.Reverse();
        else if (t?.Dir == LoopDir.PingPong) for (int f = to - 1; f > from; f--) order.Add(f);
        return order;
    }

    private static string WriteFrames(Sprite s, Tag? tag, string? root, string? name, string? source, DateTime? when)
    {
        name = string.IsNullOrWhiteSpace(name) ? tag?.Name ?? "sprite" : name;
        if (string.IsNullOrWhiteSpace(name)) name = "sprite";

        var folder = NewFolder(root ?? DefaultRoot, name, when ?? DateTime.Now);
        var files = new List<string>();
        var flat = new Image(s.Width, s.Height);
        var area = new PixelRect(0, 0, s.Width, s.Height);
        var durations = new List<int>();
        foreach (int f in FrameOrder(s, tag))
        {
            Compositor.Flatten(s, f, flat, area);
            var file = "frame_" + files.Count.ToString("000", CultureInfo.InvariantCulture) + ".png";
            File.WriteAllBytes(Path.Combine(folder, file), Png.Encode(flat));
            files.Add(file);
            durations.Add(s.Frames[f].DurationMs);
        }

        var manifest = new Dictionary<string, object?>
        {
            ["format"] = Format,
            ["name"] = name,
            ["fps"] = FpsFor(durations),
            ["frames"] = files,
            ["width"] = s.Width,
            ["height"] = s.Height,
        };
        if (!string.IsNullOrEmpty(source)) manifest["source"] = source;
        // Last, so a folder with a manifest is always complete.
        File.WriteAllText(Path.Combine(folder, ManifestName), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return folder;
    }

    private static string NewFolder(string root, string name, DateTime when)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (safe.Length == 0) safe = "sprite";
        var baseName = safe + "-" + when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        Directory.CreateDirectory(root);
        for (int n = 1; ; n++)
        {
            var folder = Path.Combine(root, n == 1 ? baseName : baseName + "-" + n.ToString(CultureInfo.InvariantCulture));
            if (Directory.Exists(folder)) continue;
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>The newest exports first (at most <paramref name="max"/>); folders without a readable manifest are skipped.</summary>
    public static IReadOnlyList<HandoffInfo> ListRecent(int max = 20, string? root = null)
    {
        root ??= DefaultRoot;
        var found = new List<HandoffInfo>();
        try
        {
            if (!Directory.Exists(root)) return found;
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var manifest = Path.Combine(dir, ManifestName);
                if (!File.Exists(manifest)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(manifest), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                    var r = doc.RootElement;
                    if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("frames", out var frames) || frames.ValueKind != JsonValueKind.Array) continue;
                    found.Add(new HandoffInfo(dir, NameOf(r, dir), FpsOf(r), frames.GetArrayLength(), File.GetLastWriteTime(manifest)));
                }
                catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException) { }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return found.OrderByDescending(i => i.When).Take(Math.Max(0, max)).ToList();
    }

    // The same rules as EasyEngine's copy (EasyPixelBridge.NameOf / FpsOf); BridgeTests.Reader_MatchesEngineRules checks them.

    /// <summary>The export's name; a missing or blank one is the folder's name.</summary>
    private static string NameOf(JsonElement r, string dir) =>
        r.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(n.GetString()) ? n.GetString()! : Path.GetFileName(dir);

    /// <summary>The speed: a number or a numeric string ("6", "6.5", "6,5"); at least 1, rounded, at most 60; 10 when unusable.</summary>
    private static int FpsOf(JsonElement r) =>
        r.TryGetProperty("fps", out var fp) && TryNumber(fp, out var v) && v >= 1 ? (int)Math.Min(60, Math.Round(v)) : 10;

    private static bool TryNumber(JsonElement e, out double v)
    {
        v = 0;
        if (e.ValueKind == JsonValueKind.Number) return e.TryGetDouble(out v);
        return e.ValueKind == JsonValueKind.String
            && double.TryParse(e.GetString()?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }
}
