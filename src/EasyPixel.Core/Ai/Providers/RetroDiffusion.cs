namespace EasyPixel.Core.Ai.Providers;

/// <summary>Retro Diffusion (paid, pixel-art specialist). Header X-RD-Token; answers {"base64_images":["…"]} (PNG).</summary>
public sealed class RetroDiffusionImage : HttpProvider, IImageGenerator
{
    internal const string Url = "https://api.retrodiffusion.ai/v1/inferences";
    internal const string Style = "rd_fast__default";
    public RetroDiffusionImage() : this(null) { }
    internal RetroDiffusionImage(HttpClient? http) : base(http) { }

    public string Id => "retrodiffusion";
    public string Name => "Retro Diffusion (paid, pixel art)";
    public bool NeedsKey => true;
    public bool Free => false;
    public int MaxParallel => 1;
    private protected override string DisplayName => "Retro Diffusion";

    public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
    {
        var k = RequireKey(key);
        using var req = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = Json(new { prompt, width = 128, height = 128, num_images = 1, seed, prompt_style = Style }),
        };
        req.Headers.Add("X-RD-Token", k);
        var json = ParseJson(await SendAsync(req, ImageTimeout, key, ct).ConfigureAwait(false));
        return FromBase64(Str(json["base64_images"]?[0]));
    }
}
