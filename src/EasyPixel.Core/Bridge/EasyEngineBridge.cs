using System.Globalization;
using System.Text.Json;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Bridge;

/// <summary>A project in EasyEngine's Recent list.</summary>
public sealed record EngineProject(string Name, string Path, DateTime LastOpened);

/// <summary>One animation of an EasyEngine object: its pictures (absolute paths of files that exist) and speed. An object with a single picture and no animations has one entry with a null <see cref="Name"/>.</summary>
public sealed record EngineAnimation(string? Name, IReadOnlyList<string> Frames, int Fps);

/// <summary>An object of an EasyEngine project that has pictures. <see cref="Scene"/> is empty for prefabs. <see cref="Warnings"/> are fixed English texts for the UI to translate.</summary>
public sealed record EngineSprite(string Scene, string Object, IReadOnlyList<EngineAnimation> Animations, IReadOnlyList<string> Warnings);

/// <summary>
/// Reads EasyEngine projects (never writes them): the Recent list, the sprites of a <c>.eeproj</c> and a sprite as an
/// EasyPixel document. Reading is tolerant: only the keys needed are looked at, unknown or missing keys are fine.
/// </summary>
public static class EasyEngineBridge
{
    public const int DefaultFps = 10;
    public const string MissingPicturesWarning = "Some pictures are missing and were skipped";
    public const string UnreadablePictureWarning = "A picture could not be read and was skipped";
    public const string DifferentSizesWarning = "Frames have different sizes";

    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    /// <summary>The projects in EasyEngine's recent list whose file still exists (newest first); empty when there is no list.</summary>
    public static IReadOnlyList<EngineProject> ListProjects(string? recentJson = null)
    {
        var result = new List<EngineProject>();
        try
        {
            var file = recentJson ?? AppDiscovery.EngineRecentFile();
            if (!File.Exists(file)) return result;
            using var doc = JsonDocument.Parse(File.ReadAllText(file), Lenient);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var path = Str(e, "Path");
                if (path == null || !File.Exists(path)) continue;
                var name = Str(e, "Name") ?? System.IO.Path.GetFileNameWithoutExtension(path);
                DateTime.TryParse(Str(e, "LastOpened"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var when);
                result.Add(new EngineProject(name, path, when));
            }
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException) { }
        return result.OrderByDescending(p => p.LastOpened).ToList();
    }

    /// <summary>
    /// The objects of the project (scenes' layers, then prefabs) that have pictures on disk. Pictures that are missing
    /// are skipped (with a warning on the sprite); an object with none left is left out.
    /// Throws <see cref="IOException"/> when the file can't be read and <see cref="InvalidDataException"/> when it isn't JSON.
    /// </summary>
    public static IReadOnlyList<EngineSprite> ListSprites(string eeproj)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(File.ReadAllText(eeproj), Lenient); }
        catch (JsonException e) { throw new InvalidDataException("Not a project file: " + e.Message, e); }
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(eeproj))!;
        var result = new List<EngineSprite>();
        using (doc)
        {
            var root = doc.RootElement;
            foreach (var scene in Items(root, "Scenes"))
                foreach (var layer in Items(scene, "Layers"))
                    foreach (var obj in Items(layer, "Objects"))
                        AddObject(result, dir, Str(scene, "Name") ?? "", obj);
            foreach (var prefab in Items(root, "Prefabs"))
                AddObject(result, dir, "", prefab);
        }
        return result;
    }

    private static void AddObject(List<EngineSprite> into, string dir, string scene, JsonElement obj)
    {
        var name = Str(obj, "Name") ?? Str(obj, "Id") ?? "Object";
        var animations = new List<EngineAnimation>();
        bool missing = false;
        foreach (var a in Items(obj, "Animations"))
        {
            var frames = ExistingFiles(dir, Strings(a, "Frames"), ref missing);
            if (frames.Count == 0) continue;
            int fps = Num(a, "Fps") is { } f && f > 0 ? (int)Math.Round(f, MidpointRounding.AwayFromZero) : DefaultFps;
            animations.Add(new EngineAnimation(Str(a, "Name") ?? "Animation " + (animations.Count + 1), frames, Math.Max(1, fps)));
        }
        if (animations.Count == 0 && Str(obj, "SpritePath") is { Length: > 0 } single)
        {
            var frames = ExistingFiles(dir, new[] { single }, ref missing);
            if (frames.Count > 0) animations.Add(new EngineAnimation(null, frames, DefaultFps));
        }
        if (animations.Count == 0) return;
        into.Add(new EngineSprite(scene, name, animations, missing ? new[] { MissingPicturesWarning } : Array.Empty<string>()));
    }

    private static List<string> ExistingFiles(string dir, IEnumerable<string> relative, ref bool missing)
    {
        var found = new List<string>();
        foreach (var rel in relative)
        {
            var path = System.IO.Path.Combine(dir, rel.Replace('\\', System.IO.Path.DirectorySeparatorChar).Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (File.Exists(path)) found.Add(path);
            else missing = true;
        }
        return found;
    }

    /// <summary>
    /// The sprite as an EasyPixel document: every picture a frame (a frame lasts 1000 / fps ms), one tag per named
    /// animation; the canvas is the largest width x largest height, each picture a cel at the top left.
    /// Null when no picture could be read. Fixed English notes go to <paramref name="warnings"/>.
    /// </summary>
    public static Sprite? ToSprite(EngineSprite source, List<string> warnings)
    {
        var cache = new Dictionary<string, Image?>();
        var images = new List<(Image Image, int DurationMs)>();
        var tags = new List<(string Name, int From, int To)>();
        foreach (var a in source.Animations)
        {
            int from = images.Count;
            int duration = (int)Math.Round(1000.0 / a.Fps, MidpointRounding.AwayFromZero);
            foreach (var path in a.Frames)
            {
                if (!cache.TryGetValue(path, out var img)) cache[path] = img = Load(path);
                if (img == null) { AddOnce(warnings, UnreadablePictureWarning); continue; }
                images.Add((img, duration));
            }
            if (a.Name != null && images.Count > from) tags.Add((a.Name, from, images.Count - 1));
        }
        if (images.Count == 0) return null;

        int w = images.Max(i => i.Image.Width), h = images.Max(i => i.Image.Height);
        if (images.Any(i => i.Image.Width != w || i.Image.Height != h)) AddOnce(warnings, DifferentSizesWarning);
        var sp = new Sprite(w, h);
        for (int i = 1; i < images.Count; i++) sp.Frames.Add(new Frame());
        for (int i = 0; i < images.Count; i++)
        {
            sp.Frames[i].DurationMs = images[i].DurationMs;
            sp.Cels[(sp.Layers[0], i)] = new Cel(images[i].Image.Clone());
        }
        foreach (var t in tags) sp.Tags.Add(new Tag { Name = t.Name, From = t.From, To = t.To });
        return sp;
    }

    private static Image? Load(string path)
    {
        try { return Png.Decode(File.ReadAllBytes(path)); }
        catch (Exception e) when (e is FormatException or InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException) { return null; }
    }

    private static void AddOnce(List<string> list, string text)
    {
        if (!list.Contains(text)) list.Add(text);
    }

    // ---- tolerant JSON reading ----

    private static string? Str(JsonElement e, string key) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? Num(JsonElement e, string key)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString()?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    private static IEnumerable<JsonElement> Items(JsonElement e, string key)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in v.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object) yield return item;
    }

    private static IEnumerable<string> Strings(JsonElement e, string key)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in v.EnumerateArray())
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } s) yield return s;
    }
}
