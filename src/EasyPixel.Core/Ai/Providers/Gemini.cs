using System.Text.Json.Nodes;

namespace EasyPixel.Core.Ai.Providers;

internal static class GeminiApi
{
    internal const string Base = "https://generativelanguage.googleapis.com/v1beta/models/";

    internal static HttpRequestMessage Request(string model, string key, object body, Func<object, StringContent> json)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}{model}:generateContent") { Content = json(body) };
        req.Headers.Add("x-goog-api-key", key);
        return req;
    }

    internal static IEnumerable<JsonNode> Parts(JsonNode json) =>
        (json["candidates"]?[0]?["content"]?["parts"] as JsonArray)?.OfType<JsonNode>() ?? [];
}

/// <summary>Gemini image (paid; no free tier for image models). The picture comes back in a part's inlineData.data.</summary>
public sealed class GeminiImage : HttpProvider, IImageGenerator
{
    internal const string Model = "gemini-2.5-flash-image";
    public GeminiImage() : this(null) { }
    internal GeminiImage(HttpClient? http) : base(http) { }

    public string Id => "gemini";
    public string Name => "Google Gemini (paid)";
    public bool NeedsKey => true;
    public bool Free => false;
    public int MaxParallel => 2;
    private protected override string DisplayName => "Gemini";

    public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { responseModalities = new[] { "IMAGE" }, seed },
        };
        using var req = GeminiApi.Request(Model, k, body, Json);
        var json = ParseJson(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
        var data = GeminiApi.Parts(json).Select(p => Str(p["inlineData"]?["data"]) ?? Str(p["inline_data"]?["data"]))
                                        .FirstOrDefault(d => d != null);
        return FromBase64(data);
    }
}

/// <summary>Gemini text for translation (free tier with a key).</summary>
public sealed class GeminiText : HttpProvider, ITextAi
{
    internal const string Model = "gemini-2.5-flash";
    public GeminiText() : this(null) { }
    internal GeminiText(HttpClient? http) : base(http) { }

    public string Id => "gemini";
    public bool NeedsKey => true;
    private protected override string DisplayName => "Gemini";

    public async Task<string> AskAsync(string system, string user, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = system } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = user } } } },
        };
        using var req = GeminiApi.Request(Model, k, body, Json);
        var json = ParseJson(await SendAsync(req, TextTimeout, key, ct).ConfigureAwait(false));
        return RequireText(string.Concat(GeminiApi.Parts(json).Select(p => Str(p["text"]))));
    }
}
