namespace EasyPixel.Core.Ai.Providers;

/// <summary>Cloudflare Workers AI. The key is one string "accountId:apiToken" (both come from the Cloudflare dashboard).</summary>
internal static class CloudflareKey
{
    internal const string Base = "https://api.cloudflare.com/client/v4/accounts/";

    internal static (string Account, string Token) Split(string key, string name)
    {
        int i = key.IndexOf(':');
        if (i <= 0 || i == key.Length - 1)
            throw new AiException(AiErrorKind.KeyFormat, name);
        return (key[..i].Trim(), key[(i + 1)..].Trim());
    }
}

/// <summary>Free tier (10,000 neurons/day): FLUX.1 schnell, answers JSON {"result":{"image":"&lt;base64&gt;"}}.</summary>
public sealed class CloudflareImage : HttpProvider, IImageGenerator
{
    internal const string Model = "@cf/black-forest-labs/flux-1-schnell";
    public CloudflareImage() : this(null) { }
    internal CloudflareImage(HttpClient? http) : base(http) { }

    public string Id => "cloudflare";
    public string Name => "Cloudflare Workers AI (free)";
    public bool NeedsKey => true;
    public bool Free => true;
    public int MaxParallel => 2;
    private protected override string DisplayName => "Cloudflare";

    public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
    {
        var (account, token) = CloudflareKey.Split(RequireKey(key), DisplayName);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{CloudflareKey.Base}{Uri.EscapeDataString(account)}/ai/run/{Model}")
        {
            Content = Json(new { prompt, steps = 8 }), // 8 = the most it allows (clearer shapes); it refuses "seed" and width/height, each call is random anyway
        };
        Bearer(req, token);
        var json = ParseJson(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
        return FromBase64(Str(json["result"]?["image"]) ?? Str(json["image"]));
    }
}

/// <summary>Free tier text model for translation; answers {"result":{"response":"…"}}.</summary>
public sealed class CloudflareText : HttpProvider, ITextAi
{
    internal const string Model = "@cf/meta/llama-3.1-8b-instruct-fp8";
    public CloudflareText() : this(null) { }
    internal CloudflareText(HttpClient? http) : base(http) { }

    public string Id => "cloudflare";
    public bool NeedsKey => true;
    private protected override string DisplayName => "Cloudflare";

    public async Task<string> AskAsync(string system, string user, string? key, CancellationToken ct)
    {
        var (account, token) = CloudflareKey.Split(RequireKey(key), DisplayName);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{CloudflareKey.Base}{Uri.EscapeDataString(account)}/ai/run/{Model}")
        {
            Content = Json(new { messages = new[] { new { role = "system", content = system }, new { role = "user", content = user } } }),
        };
        Bearer(req, token);
        var json = ParseJson(await SendAsync(req, TextTimeout, key, ct).ConfigureAwait(false));
        return RequireText(Str(json["result"]?["response"]));
    }
}
