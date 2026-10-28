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
        var culture = CultureInfo.CurrentUICulture;
        foreach (var c in new[] { culture.Name, ChineseScript(culture), culture.TwoLetterISOLanguageName })
            if (!string.IsNullOrEmpty(c) && ReadEmbedded(c) != null) return c;
        return culture.TwoLetterISOLanguageName == "pt" ? "pt-BR" : "en";
    }

    /// <summary>zh-CN / zh-SG use simplified characters, zh-TW / zh-HK / zh-MO traditional.</summary>
    private static string? ChineseScript(CultureInfo c)
    {
        if (c.TwoLetterISOLanguageName != "zh") return null;
        var n = c.Name;
        return n.Contains("Hant") || n.EndsWith("TW") || n.EndsWith("HK") || n.EndsWith("MO") ? "zh-Hant" : "zh-Hans";
    }

    public static string T(string english) =>
        english != null && _table.TryGetValue(english, out var t) ? t : english ?? "";

    public static string F(string englishFormat, params object[] args)
    {
        try { return string.Format(CultureInfo.CurrentCulture, T(englishFormat), args); }
        catch (FormatException) { return string.Format(CultureInfo.InvariantCulture, englishFormat, args); }
    }

    /// <summary>The languages EasyPixel ships, with their own names (shown untranslated in the language picker).</summary>
    public static readonly (string Code, string Name)[] Languages =
    {
        ("en", "English"), ("de", "Deutsch"), ("es", "Español"), ("fr", "Français"), ("ja", "日本語"),
        ("pt-BR", "Português (Brasil)"), ("pt-PT", "Português (Portugal)"), ("zh-Hans", "简体中文"), ("zh-Hant", "繁體中文"),
    };

    /// <summary>The class that marks a control showing the person's own content (layer and tag names, file paths) or a language name: never translated.</summary>
    public const string KeepClass = "keep";

    /// <summary>Marks <paramref name="c"/> as content that is not translated (see <see cref="KeepClass"/>).</summary>
    public static T Keep<T>(T c) where T : Avalonia.StyledElement
    {
        c.Classes.Add(KeepClass);
        return c;
    }

    /// <summary>Uses <paramref name="table"/> as the translation table (tests).</summary>
    internal static void UseTable(Dictionary<string, string> table, string code)
    {
        _table = table;
        Language = code;
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
