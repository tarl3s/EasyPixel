using EasyPixel.Core.Ai;

namespace EasyPixel.Ai;

/// <summary>Which providers a request goes to, from the person's settings.</summary>
public static class AiRouting
{
    public static bool HasKey(Settings s, string id) => s.AiKeys.TryGetValue(id, out var k) && !string.IsNullOrWhiteSpace(k);

    /// <summary>"auto" = the free generators that have a key (keyless ones first), in list order — empty when none; a chosen id = that one.</summary>
    public static IReadOnlyList<IImageGenerator> ImageGenerators(Settings s)
    {
        if (s.AiImage != "auto")
            return AiProviders.Image(s.AiImage) is { } one ? [one] : [];
        var free = AiProviders.Images.Where(p => p.Free && (!p.NeedsKey || HasKey(s, p.Id))).ToList();
        return free.Where(p => !p.NeedsKey).Concat(free.Where(p => p.NeedsKey)).ToList();
    }

    /// <summary>The generators a request can really use: <see cref="ImageGenerators"/> without a chosen one that still lacks its key.</summary>
    public static IReadOnlyList<IImageGenerator> UsableGenerators(Settings s) =>
        ImageGenerators(s).Where(p => !p.NeedsKey || HasKey(s, p.Id)).ToList();

    /// <summary>A Create with AI run with the person's current choice and keys.</summary>
    public static AiCreateSession NewSession() => new(UsableGenerators(App.Settings), TextAi(App.Settings),
        id => App.Settings.AiKeys.TryGetValue(id, out var k) ? k : null);

    /// <summary>The chosen text AI; "auto" = one that has a key, preferring the image provider's (Cloudflare first).</summary>
    public static ITextAi? TextAi(Settings s)
    {
        if (s.AiText != "auto") return AiProviders.Text(s.AiText);
        var order = ImageGenerators(s).Select(p => p.Id).ToList();
        int Rank(ITextAi t) { int i = order.IndexOf(t.Id); return i < 0 ? int.MaxValue : i; }
        return AiProviders.Texts.Where(t => !t.NeedsKey || HasKey(s, t.Id)).OrderBy(Rank).FirstOrDefault();
    }
}
