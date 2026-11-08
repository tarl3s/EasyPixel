using System.Text.RegularExpressions;

namespace EasyPixel.Core.Ai;

/// <summary>Which way the generated sprite faces.</summary>
public enum SpriteView { Auto, Side, Front, TopDown }

/// <summary>Builds the text sent to an image generator: the (English) request, then a fixed pixel-art style template.</summary>
public static class SpritePrompt
{
    private const int MaxRequest = 1000;

    private static readonly Regex Objects = new(
        @"\b(chest|door|potion|sword|coin|house|building|school|tree|sign|icon|item|key|gem|crate|barrel|castle|tower|shield|book|bottle|lamp|torch|statue|fountain|bed|table|chair)s?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Auto picks Front for objects, items and buildings and Side for everything else; an explicit choice wins.</summary>
    public static SpriteView ResolveView(string englishRequest, SpriteView chosen)
    {
        if (chosen != SpriteView.Auto) return chosen;
        return Objects.IsMatch(englishRequest ?? "") ? SpriteView.Front : SpriteView.Side;
    }

    public static string Build(string englishRequest, SpriteView view)
    {
        var req = (englishRequest ?? "").Trim();
        if (req.Length > MaxRequest) req = req[..MaxRequest];
        var v = ResolveView(req, view) switch
        {
            SpriteView.Front => "front view",
            SpriteView.TopDown => "top-down view",
            _ => "side view",
        };
        // The subject comes first and is said plainly (image models weigh the start most); the style stays short so it
        // doesn't drown the details the person asked for.
        return $"Pixel art game sprite of {Soften(req)}. One single creature or object, full body, {v}, centred on a plain flat " +
               "white background. Bold dark outline, few colours, simple shading, Terraria and Stardew Valley style. " +
               "Nothing else in the picture: no ground, no shadow, no puddle, no props, no text, no frame.";
    }

    private static readonly (Regex Words, string Instead)[] Gore =
    {
        (new(@"\b(blood[- ]?(stained|soaked|covered|smeared)|bloody|bloodied|bleeding)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "red-stained"),
        (new(@"\b(blood|gore)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "red stains"),
    };

    /// <summary>
    /// Words some generators refuse as "too violent" (Cloudflare blocks "blood") swapped for ones that draw the same
    /// thing in pixel art: a few dark red pixels.
    /// </summary>
    public static string Soften(string englishRequest)
    {
        var s = englishRequest ?? "";
        foreach (var (words, instead) in Gore) s = words.Replace(s, instead);
        return s;
    }
}
