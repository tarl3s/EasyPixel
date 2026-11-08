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
    // Interactions API (the image docs' current shape); the seed has no documented field and is not sent.
    internal const string Url = "https://generativelanguage.googleapis.com/v1beta/interactions";
    internal const string Model = "gemini-nano-banana-2.1";
    internal const string Revision = "2026-05-20";
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
        using var req = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = Json(new
            {
                model = Model,
                input = prompt,
                response_format = new { type = "image", mime_type = "image/png", aspect_ratio = "1:1" },
            }),
        };
        req.Headers.Add("x-goog-api-key", k);
        req.Headers.Add("Api-Revision", Revision);
        var json = ParseJson(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
        return FromBase64(Str(json["output_image"]?["data"]));
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
