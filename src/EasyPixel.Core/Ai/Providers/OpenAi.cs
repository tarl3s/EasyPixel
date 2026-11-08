namespace EasyPixel.Core.Ai.Providers;

/// <summary>OpenAI Images (paid). gpt-image-1 always answers base64 PNG in data[0].b64_json.</summary>
public sealed class OpenAiImage : HttpProvider, IImageGenerator
{
    internal const string Url = "https://api.openai.com/v1/images/generations";
    internal const string Model = "gpt-image-1";
    public OpenAiImage() : this(null) { }
    internal OpenAiImage(HttpClient? http) : base(http) { }

    public string Id => "openai";
    public string Name => "OpenAI (paid)";
    public bool NeedsKey => true;
    public bool Free => false;
    public int MaxParallel => 2;
    private protected override string DisplayName => "OpenAI";

    // The API has no seed; the seed is ignored (each call is already different).
    public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        using var req = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = Json(new { model = Model, prompt, size = "1024x1024", quality = "low", output_format = "png", n = 1 }),
        };
        Bearer(req, k);
        var json = ParseJson(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
        return FromBase64(Str(json["data"]?[0]?["b64_json"]));
    }
}

/// <summary>OpenAI chat completions for translation (paid).</summary>
public sealed class OpenAiText : HttpProvider, ITextAi
{
    internal const string Url = "https://api.openai.com/v1/chat/completions";
    internal const string Model = "gpt-4o-mini";
    public OpenAiText() : this(null) { }
    internal OpenAiText(HttpClient? http) : base(http) { }

    public string Id => "openai";
    public bool NeedsKey => true;
    private protected override string DisplayName => "OpenAI";

    public async Task<string> AskAsync(string system, string user, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        using var req = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = Json(new { model = Model, messages = new[] { new { role = "system", content = system }, new { role = "user", content = user } } }),
        };
        Bearer(req, k);
        var json = ParseJson(await SendAsync(req, TextTimeout, key, ct).ConfigureAwait(false));
        return RequireText(Str(json["choices"]?[0]?["message"]?["content"]));
    }
}
