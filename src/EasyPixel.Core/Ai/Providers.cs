using System.Runtime.CompilerServices;
using EasyPixel.Core.Ai.Providers;

[assembly: InternalsVisibleTo("EasyPixel.Tests")]
[assembly: InternalsVisibleTo("EasyPixel.UiTests")]

namespace EasyPixel.Core.Ai;

/// <summary>Turns a text prompt into a picture. Returns the raw image bytes (PNG where the API allows).</summary>
public interface IImageGenerator
{
    string Id { get; }
    string Name { get; }
    bool NeedsKey { get; }
    bool Free { get; }
    int MaxParallel { get; }
    /// <summary>Throws <see cref="AiException"/> on any failure (never containing the key); a cancel surfaces as OperationCanceledException.</summary>
    Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct);
}

/// <summary>A text AI (used to translate requests to English).</summary>
public interface ITextAi
{
    string Id { get; }
    bool NeedsKey { get; }
    Task<string> AskAsync(string system, string user, string? key, CancellationToken ct);
}

/// <summary>A short, friendly English message for the person; never contains an API key.</summary>
public sealed class AiException(string message) : Exception(message);

/// <summary>
/// Every provider the editor offers; free ones come first. Cloudflare keys are one string "accountId:apiToken".
/// </summary>
public static class AiProviders
{
    private static readonly IReadOnlyList<IImageGenerator> DefaultImages =
    [
        new CloudflareImage(), new PollinationsImage(),
        new OpenAiImage(), new GeminiImage(), new RetroDiffusionImage(),
    ];
    private static readonly IReadOnlyList<ITextAi> DefaultTexts = [new CloudflareText(), new GeminiText(), new OpenAiText()];

    public static IReadOnlyList<IImageGenerator> Images { get; private set; } = DefaultImages;
    public static IReadOnlyList<ITextAi> Texts { get; private set; } = DefaultTexts;

    public static IImageGenerator? Image(string id) => Images.FirstOrDefault(p => p.Id == id);
    public static ITextAi? Text(string id) => Texts.FirstOrDefault(p => p.Id == id);

    /// <summary>Tests: replace the lists (null keeps the default). Dispose the result to restore both.</summary>
    internal static IDisposable UseForTests(IReadOnlyList<IImageGenerator>? images, IReadOnlyList<ITextAi>? texts = null)
    {
        Images = images ?? DefaultImages;
        Texts = texts ?? DefaultTexts;
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() { Images = DefaultImages; Texts = DefaultTexts; }
    }
}
