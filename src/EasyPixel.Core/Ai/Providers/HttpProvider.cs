using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EasyPixel.Core.Ai.Providers;

/// <summary>Shared HTTP plumbing: timeouts, cancellation, friendly errors with the key scrubbed.</summary>
public abstract class HttpProvider
{
    internal static readonly TimeSpan ImageTimeout = TimeSpan.FromSeconds(90);
    internal static readonly TimeSpan TextTimeout = TimeSpan.FromSeconds(30);
    private static readonly HttpClient SharedClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    private protected readonly HttpClient Http;
    private protected abstract string DisplayName { get; }

    private protected HttpProvider(HttpClient? http) => Http = http ?? SharedClient;

    private protected string RequireKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new AiException($"{DisplayName} needs an API key. Add it in the AI settings.");
        return key.Trim();
    }

    private protected static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private protected static void Bearer(HttpRequestMessage req, string key) =>
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

    /// <summary>Sends the request and returns the body of a 2xx answer; anything else becomes an AiException.</summary>
    private protected async Task<byte[]> SendAsync(HttpRequestMessage req, TimeSpan timeout, string? key, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            using var resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
            var body = await resp.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) throw new AiException(Scrub(StatusMessage(resp.StatusCode, body), key));
            if (body.Length == 0) throw new AiException($"{DisplayName} sent an empty answer. Try again.");
            return body;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new AiException($"{DisplayName} took too long to answer. Try again."); }
        catch (HttpRequestException) { throw new AiException($"Couldn't reach {DisplayName}. Check the internet connection."); }
    }

    private string StatusMessage(HttpStatusCode code, byte[] body) => (int)code switch
    {
        401 or 403 => $"{DisplayName} refused the API key. Check it in the AI settings.",
        402 => $"{DisplayName} says the account has no credit left.",
        429 => $"{DisplayName} is busy or the free quota is used up. Try again later.",
        400 => $"{DisplayName} didn't accept the request{Detail(body)}",
        >= 500 => $"{DisplayName} had a problem on its side. Try again later.",
        _ => $"{DisplayName} answered with an error ({(int)code}).",
    };

    /// <summary>A short error detail from a JSON body ("error.message" / "errors[0].message"), else nothing.</summary>
    private static string Detail(byte[] body)
    {
        try
        {
            var n = JsonNode.Parse(body);
            string? msg = null;
            if (n?["error"] is JsonObject eo) msg = eo["message"]?.ToString();
            else if (n?["error"] is JsonValue ev) msg = ev.ToString();
            else if (n?["errors"] is JsonArray ea && ea.Count > 0) msg = ea[0]?["message"]?.ToString();
            if (string.IsNullOrWhiteSpace(msg)) return ".";
            msg = msg.Trim();
            return ": " + (msg.Length > 160 ? msg[..160] + "…" : msg);
        }
        catch (Exception) { return "."; }
    }

    /// <summary>Removes the key (and each part of an "a:b" key) from any text.</summary>
    internal static string Scrub(string text, string? key)
    {
        if (string.IsNullOrEmpty(key)) return text;
        foreach (var part in key.Split(':').Append(key.Trim()).OrderByDescending(p => p.Length))
            if (part.Length >= 4) text = text.Replace(part, "***", StringComparison.Ordinal);
        return text;
    }

    private protected JsonNode ParseJson(byte[] body)
    {
        try { return JsonNode.Parse(body) ?? throw new JsonException(); }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
        { throw new AiException($"{DisplayName} sent an answer EasyPixel can't read. Try again."); }
    }

    private protected byte[] FromBase64(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64)) throw new AiException($"{DisplayName} didn't send a picture. Try again or change the description.");
        int comma = b64.IndexOf(',');
        if (b64.StartsWith("data:", StringComparison.Ordinal) && comma > 0) b64 = b64[(comma + 1)..];
        try { return RequireImage(Convert.FromBase64String(b64)); }
        catch (FormatException) { throw new AiException($"{DisplayName} sent a broken picture. Try again."); }
    }

    /// <summary>Raw image bytes must really be a picture (not an HTML error page).</summary>
    private protected byte[] RequireImage(byte[] b)
    {
        bool png = b.Length > 8 && b[0] == 0x89 && b[1] == (byte)'P' && b[2] == (byte)'N' && b[3] == (byte)'G';
        bool jpg = b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8;
        bool webp = b.Length > 12 && b[0] == (byte)'R' && b[1] == (byte)'I' && b[8] == (byte)'W' && b[9] == (byte)'E';
        if (!png && !jpg && !webp) throw new AiException($"{DisplayName} didn't send a picture. Try again later.");
        return b;
    }

    private protected static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private protected string RequireText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new AiException($"{DisplayName} sent an empty answer. Try again.");
        return text.Trim();
    }
}
