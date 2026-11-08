namespace EasyPixel.Core.Ai.Providers;

/// <summary>Pollinations with a key (enter.pollinations.ai): GET returns the image bytes directly.</summary>
public sealed class PollinationsImage : HttpProvider, IImageGenerator
{
    internal const string Base = "https://gen.pollinations.ai/image/";
    internal const string Model = "black-forest-labs/flux.1-schnell";
    public PollinationsImage() : this(null) { }
    internal PollinationsImage(HttpClient? http) : base(http) { }

    public string Id => "pollinations";
    public string Name => "Pollinations (free key)";
    public bool NeedsKey => true;
    public bool Free => true;
    public int MaxParallel => 1;
    private protected override string DisplayName => "Pollinations";

    public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        var p = prompt.Length > 600 ? prompt[..600] : prompt; // keep the URL short (encoded <= ~1800 chars)
        var url = $"{Base}{Uri.EscapeDataString(p)}?model={Model}&width=512&height=512&seed={seed}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        Bearer(req, k);
        return RequireImage(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
    }
}
