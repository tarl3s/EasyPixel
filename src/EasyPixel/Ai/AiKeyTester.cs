using EasyPixel.Core.Ai;

namespace EasyPixel.Ai;

/// <summary>The settings window's Test button: one cheap call; the answer is a short line that never contains the key.</summary>
public static class AiKeyTester
{
    public static async Task<(bool Ok, string Message)> TestAsync(string providerId, string key, CancellationToken ct = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            if (AiProviders.Text(providerId) is { } text)
                await text.AskAsync("Reply with the single word OK.", "ping", key, timeout.Token);
            else if (AiProviders.Image(providerId) is { } image)
                await image.GenerateAsync("a small red dot", 1, key, timeout.Token);
            else
                return (false, L.T("Unknown provider."));
            return (true, L.T("It works."));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (false, L.T("No answer in time."));
        }
        catch (AiException e) { return (false, AiErrors.Describe(e, key)); }
        catch (Exception) { return (false, L.T("Could not reach the service.")); }
    }

    /// <summary>Removes the key (and, for Cloudflare, its two halves) from a message.</summary>
    internal static string Scrub(string message, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return message;
        if (key.Trim().Length >= 4) message = message.Replace(key.Trim(), "…");
        foreach (var part in key.Split(':', StringSplitOptions.RemoveEmptyEntries))
            if (part.Length >= 4) message = message.Replace(part, "…");
        return message;
    }
}
