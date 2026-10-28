using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Avalonia.Markup.Xaml;

namespace EasyPixel;

/// <summary>
/// UI translation. Every visible text is written in English and passed through <see cref="T"/> (or <see cref="F"/> for a
/// pattern with <c>{0}</c>). Tables are the embedded <c>Lang/&lt;code&gt;.json</c> files ("English": "translation");
/// a missing or empty entry, or a broken table, shows the English text.
/// </summary>
public static class L
{
    private static Dictionary<string, string> _table = new(StringComparer.Ordinal);

    /// <summary>The loaded language code ("en" when none).</summary>
    public static string Language { get; private set; } = "en";

    /// <summary>Loads the embedded table for <paramref name="code"/>; an unknown code falls back to English.</summary>
    public static void Load(string code)
    {
        var json = ReadEmbedded(code);
        _table = Parse(json);
        Language = json == null ? "en" : code;
    }

    /// <summary>The language to use for a setting: a known code as chosen, "auto" (or anything else) follows the system.</summary>
    public static string Resolve(string? setting)
    {
        if (!string.IsNullOrEmpty(setting) && setting != "auto" && ReadEmbedded(setting) != null) return setting;
        var name = CultureInfo.CurrentUICulture.Name;
        return ReadEmbedded(name) != null ? name : "en";
    }

    public static string T(string english) =>
        english != null && _table.TryGetValue(english, out var t) ? t : english ?? "";

    public static string F(string englishFormat, params object[] args)
    {
        try { return string.Format(CultureInfo.CurrentCulture, T(englishFormat), args); }
        catch (FormatException) { return string.Format(CultureInfo.InvariantCulture, englishFormat, args); }
    }

    internal static string? ReadEmbedded(string code)
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("EasyPixel.Lang." + code + ".json");
        if (s == null) return null;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static Dictionary<string, string> Parse(string? json)
    {
        var t = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json)) return t;
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            foreach (var p in doc.RootElement.EnumerateObject())
                if (!p.Name.StartsWith('_') && p.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.Value.GetString()))
                    t[p.Name] = p.Value.GetString()!;
        }
        catch (JsonException) { t.Clear(); }
        return t;
    }
}

/// <summary>XAML text through the translation table: <c>Header="{loc:T 'Open…'}"</c> (with <c>xmlns:loc="using:EasyPixel"</c>).</summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }
    public TExtension(string text) => Text = text;
    public string Text { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}
