using System.Collections.Concurrent;
using System.Reflection;

namespace EasyPixel.Core.Learn;

/// <summary>A lesson as loaded for a language; <see cref="IsFallback"/> when it isn't translated and English is shown.</summary>
public sealed record LessonLoad(Lesson Lesson, bool IsFallback);

/// <summary>
/// The lessons shipped as embedded resources <c>Learn/Lessons/&lt;lang&gt;/NN-slug.md</c>. English is the master list:
/// every lesson exists in English; another language shows its own file when it has one, English otherwise.
/// </summary>
public static class LessonLibrary
{
    public const string English = "en";
    private const string Prefix = "Lessons/";

    private static readonly Assembly Asm = typeof(LessonLibrary).Assembly;
    private static readonly ConcurrentDictionary<(string, string), Lesson?> Cache = new();

    /// <summary>(language, id) → resource name, normalised to forward slashes.</summary>
    private static readonly Lazy<Dictionary<(string Lang, string Id), string>> Resources = new(() =>
    {
        var map = new Dictionary<(string, string), string>();
        foreach (var name in Asm.GetManifestResourceNames())
        {
            var n = name.Replace('\\', '/');
            if (!n.StartsWith(Prefix, StringComparison.Ordinal) || !n.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = n[Prefix.Length..].Split('/');
            if (parts.Length != 2) continue;
            map[(parts[0].ToLowerInvariant(), Path.GetFileNameWithoutExtension(parts[1]))] = name;
        }
        return map;
    });

    /// <summary>Every lesson id, in order (the English files, sorted by name: the number prefix sets the order).</summary>
    public static IReadOnlyList<string> Ids =>
        Resources.Value.Keys.Where(k => k.Lang == English).Select(k => k.Id).OrderBy(i => i, StringComparer.Ordinal).ToList();

    /// <summary>The languages that have at least one lesson file.</summary>
    public static IReadOnlyList<string> Languages => Resources.Value.Keys.Select(k => k.Lang).Distinct().OrderBy(l => l).ToList();

    /// <summary>True when <paramref name="language"/> has its own file for <paramref name="id"/>.</summary>
    public static bool HasTranslation(string id, string language) => Resources.Value.ContainsKey((Norm(language), id));

    /// <summary>
    /// The lesson in <paramref name="language"/>, or the English one (flagged <see cref="LessonLoad.IsFallback"/>) when
    /// that language has no file for it. Throws <see cref="KeyNotFoundException"/> for an id with no English file.
    /// </summary>
    public static LessonLoad Load(string id, string language)
    {
        var lang = Norm(language);
        if (lang != English && Get(lang, id) is { } own) return new LessonLoad(own, false);
        var en = Get(English, id) ?? throw new KeyNotFoundException("No lesson '" + id + "'.");
        return new LessonLoad(en, lang != English);
    }

    /// <summary>Every lesson for <paramref name="language"/>, in order.</summary>
    public static IReadOnlyList<LessonLoad> All(string language) => Ids.Select(id => Load(id, language)).ToList();

    /// <summary>The raw markup of a lesson file, or null.</summary>
    public static string? ReadRaw(string language, string id)
    {
        if (!Resources.Value.TryGetValue((Norm(language), id), out var name)) return null;
        using var s = Asm.GetManifestResourceStream(name);
        if (s == null) return null;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static Lesson? Get(string lang, string id) =>
        Cache.GetOrAdd((lang, id), k => ReadRaw(k.Item1, k.Item2) is { } text ? LessonParser.Parse(k.Item2, text) : null);

    private static string Norm(string? language) => string.IsNullOrWhiteSpace(language) ? English : language.Trim().ToLowerInvariant();
}
