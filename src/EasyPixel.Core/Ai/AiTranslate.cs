using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyPixel.Core.Ai;

/// <summary>
/// A request in another language is first turned into English by the same AI (image generators follow English far better).
/// Names, numbers and quoted text stay as written; a translation that loses any of them is not used.
/// </summary>
public static class AiTranslate
{
    internal const string System =
        "Translate the user's request for a game sprite into plain English, as a short description (\"a wolf with red eyes\"). " +
        "Keep every name that starts with a capital letter, every number and every text inside quotes exactly as written. " +
        "Translate the meaning, not word by word. " +
        "Answer ONLY with JSON, e.g. for \"um lobo com olhos vermelhos\": {\"english\":\"a wolf with red eyes\"}";

    private static readonly Regex WordRx = new(@"\p{L}+'?\p{L}*", RegexOptions.Compiled);
    private static readonly Regex QuotedRx = new("[\"“‘«']([^\"”’»']{2,})[\"”’»']", RegexOptions.Compiled);
    private static readonly HashSet<string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "an", "and", "or", "to", "of", "when", "make", "it", "is", "with", "every", "that", "on", "in", "at", "from",
        "after", "i", "me", "my", "create", "add", "give", "put", "let", "its", "into", "be", "while", "only", "if", "then", "each",
        "him", "his", "her", "them", "up", "down", "can", "has", "have", "this", "for", "until", "but", "not", "don't", "build",
        "turn", "set", "also", "a", "like", "looks", "walks", "two", "red", "eyes", "legs", "wolf", "monster",
    };
    private static readonly HashSet<string> Foreign = new(StringComparer.OrdinalIgnoreCase)
    {
        // Portuguese
        "o", "os", "as", "um", "uma", "que", "de", "do", "da", "dos", "das", "para", "pra", "pro", "com", "quando", "faz", "faça", "cria",
        "crie", "coloca", "coloque", "deixa", "deixe", "ele", "ela", "no", "na", "em", "e", "é", "ao", "à", "se", "não", "mais",
        "depois", "cada", "vai", "fica", "eu", "parecido", "tem", "parece", "olho", "boca", "duas", "dois",
        // Spanish
        "el", "la", "los", "las", "y", "con", "cuando", "haz", "pon", "después", "del", "al", "lo", "su", "muy", "hacia", "crea", "tiene", "ojos",
        // French
        "le", "les", "et", "une", "des", "quand", "fais", "après", "chaque", "avec", "il", "elle", "du",
        // German
        "der", "die", "das", "und", "mit", "wenn", "ein", "eine", "nach", "mach",
        // Italian
        "gli", "dopo", "ogni", "fai",
    };

    /// <summary>True when the request is (mostly) English. Quoted text and names don't count; a non-Latin script is never English.</summary>
    public static bool LooksEnglish(string request)
    {
        var text = QuotedRx.Replace(request ?? "", " ");
        if (text.Any(c => c > 0x24F && char.IsLetter(c))) return false;
        int en = 0, other = 0;
        foreach (Match m in WordRx.Matches(text))
        {
            if (char.IsUpper(m.Value[0]) && m.Index > 0) continue;
            if (English.Contains(m.Value)) en++;
            if (Foreign.Contains(m.Value)) other++;
        }
        return other == 0 || en > other;
    }

    /// <summary>The model's translation if it can be trusted, else null: not absurdly long or short, keeping every number,
    /// quoted text and name (a capitalised word inside a sentence) of the original.</summary>
    public static string? Clean(string original, string reply)
    {
        var s = (reply ?? "").Trim();
        int a = s.IndexOf('{'), b = s.LastIndexOf('}');
        if (a >= 0 && b > a)
        {
            try
            {
                using var doc = JsonDocument.Parse(s[a..(b + 1)], new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                var root = doc.RootElement;
                s = root.ValueKind != JsonValueKind.Object ? ""
                    : (root.TryGetProperty("english", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString()
                       : root.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.String).Select(p => p.Value.GetString()).FirstOrDefault()) ?? "";
            }
            catch (JsonException) { return null; }
        }
        else
        {
            // plain-text chatter: Here is the translation: "a wolf"
            s = Regex.Replace(s, @"^(?:here is|here's|sure|translation|english)[^:\n]*:\s*", "", RegexOptions.IgnoreCase);
            s = s.Trim().Trim('"', '“', '”', '\'').Trim();
        }
        if (s.Contains("sprite into plain English", StringComparison.OrdinalIgnoreCase)) return null; // the prompt copied back
        s = Regex.Replace(s, @"\s*\n\s*", " ").Trim();
        if (s.Length < 3 || s.Length > original.Length * 3 + 40) return null;
        foreach (Match m in Regex.Matches(original, @"\d+(?:[.,]\d+)?"))
            if (!s.Contains(m.Value) && !s.Contains(m.Value.Replace(',', '.'))) return null;
        foreach (Match m in QuotedRx.Matches(original)) if (!s.Contains(m.Groups[1].Value)) return null;
        foreach (Match m in Regex.Matches(original, @"(?<![.!?:;]\s*|^\s*)(?<=\s)\p{Lu}[\p{L}\d_]*"))
            if (!Regex.IsMatch(s, @"(?<![\p{L}\d])" + Regex.Escape(m.Value) + @"(?![\p{L}\d])")) return null;
        return s;
    }

    /// <summary>The request in English; the original when it already is, or on any failure (error, timeout, untrusted translation).</summary>
    public static async Task<string> ToEnglishAsync(string request, Func<string, string, CancellationToken, Task<string>> ask, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request) || LooksEnglish(request)) return request;
        try { return Clean(request, await ask(System, request, ct)) ?? request; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return request; }
    }
}
