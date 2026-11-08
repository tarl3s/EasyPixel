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

/// <summary>What went wrong, so the app can say it in the person's language.</summary>
public enum AiErrorKind { Other, Refused, NeedsKey, KeyFormat, Auth, NoCredit, Quota, BadRequest, ServerError, HttpError, Empty, Timeout, Offline, Unreadable, NoPicture, BrokenPicture, Failed }

/// <summary>
/// A provider failure: its <see cref="Kind"/>, the provider's name and an optional (key-free) <see cref="Detail"/>; the English
/// <see cref="Exception.Message"/> is for logs and tests. Never contains an API key.
/// </summary>
public sealed class AiException : Exception
{
    public AiErrorKind Kind { get; }
    public string? ProviderName { get; }
    public string? Detail { get; }

    public AiException(string message) : base(message) { }

    public AiException(AiErrorKind kind, string provider, string? detail = null) : base(English(kind, provider, detail))
    { Kind = kind; ProviderName = provider; Detail = detail; }

    private static string English(AiErrorKind kind, string p, string? d) => kind switch
    {
        AiErrorKind.NeedsKey => $"{p} needs an API key. Add it in the AI settings.",
        AiErrorKind.KeyFormat => $"{p} needs your Account ID and API token, written as accountid:token.",
        AiErrorKind.Auth => $"{p} refused the API key. Check it in the AI settings.",
        AiErrorKind.NoCredit => $"{p} says the account has no credit left.",
        AiErrorKind.Quota => $"{p} is busy or the free quota is used up. Try again later.",
        AiErrorKind.Refused => $"{p} refused the description as too violent or adult.",
        AiErrorKind.BadRequest => string.IsNullOrEmpty(d) ? $"{p} didn't accept the request." : $"{p} didn't accept the request: {d}",
        AiErrorKind.ServerError => $"{p} had a problem on its side. Try again later.",
        AiErrorKind.HttpError => $"{p} answered with an error ({d}).",
        AiErrorKind.Empty => $"{p} sent an empty answer. Try again.",
        AiErrorKind.Timeout => $"{p} took too long to answer. Try again.",
        AiErrorKind.Offline => $"Couldn't reach {p}. Check the internet connection.",
        AiErrorKind.Unreadable => $"{p} sent an answer EasyPixel can't read. Try again.",
        AiErrorKind.NoPicture => $"{p} didn't send a picture. Try again or change the description.",
        AiErrorKind.BrokenPicture => $"{p} sent a broken picture. Try again.",
        AiErrorKind.Failed => $"{p} couldn't make this picture. Try again or change the description.",
        _ => $"{p} failed.",
    };
}

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
