using System.Text.Json.Nodes;

namespace EasyPixel.Core.Ai.Providers;

/// <summary>
/// Retro Diffusion v2 (paid, pixel-art specialist). Header X-RD-Token. POST /v2/inferences answers {"task_id"}; the task is
/// polled (with backoff, inside the 90 s image timeout) until result.base64_images[] (PNG) arrives.
/// </summary>
public sealed class RetroDiffusionImage : HttpProvider, IImageGenerator
{
    internal const string Url = "https://api.retrodiffusion.ai/v2/inferences";
    internal const string TaskUrl = "https://api.retrodiffusion.ai/v2/inferences/tasks/";
    internal const string Style = "rd_fast__default";

    /// <summary>The wait between polls (tests replace it).</summary>
    internal Func<TimeSpan, CancellationToken, Task> Delay = Task.Delay;

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
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(ImageTimeout);
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = Json(new { prompt, width = 128, height = 128, num_images = 1, seed, prompt_style = Style }),
            };
            req.Headers.Add("X-RD-Token", k);
            var json = ParseJson(await SendAsync(req, ImageTimeout, key, overall.Token).ConfigureAwait(false));
            if (Done(json) is { } first) return first;
            var task = Str(json["task_id"]);
            if (string.IsNullOrWhiteSpace(task)) throw new AiException(AiErrorKind.Failed, DisplayName);

            var wait = TimeSpan.FromSeconds(1);
            while (true)
            {
                await Delay(wait, overall.Token).ConfigureAwait(false);
                wait = TimeSpan.FromSeconds(Math.Min(wait.TotalSeconds * 1.5, 5));
                using var poll = new HttpRequestMessage(HttpMethod.Get, TaskUrl + Uri.EscapeDataString(task));
                poll.Headers.Add("X-RD-Token", k);
                var status = ParseJson(await SendAsync(poll, ImageTimeout, key, overall.Token).ConfigureAwait(false));
                if (Done(status) is { } bytes) return bytes;
                var s = Str(status["status"])?.ToLowerInvariant();
                if (s is "failed" or "error" or "cancelled" or "canceled")
                    throw new AiException(AiErrorKind.Failed, DisplayName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new AiException(AiErrorKind.Timeout, DisplayName); }
    }

    /// <summary>The picture, when the answer already carries it (result.base64_images or base64_images).</summary>
    private byte[]? Done(JsonNode json)
    {
        var b64 = Str(json["result"]?["base64_images"]?[0]) ?? Str(json["base64_images"]?[0]);
        return b64 == null ? null : FromBase64(b64);
    }
}
