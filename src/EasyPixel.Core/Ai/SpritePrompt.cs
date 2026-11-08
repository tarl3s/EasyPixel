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
        return $"{req}, single game sprite, full body, {v}, centred, plain flat white background, pixel art, bold dark outline, " +
               "limited palette, simple shading, light from top-left, Terraria and Stardew Valley style, no text, no frame, no shadow on the ground";
    }
}
