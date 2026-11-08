using EasyPixel.Core.Ai;

namespace EasyPixel.Ai;

/// <summary>Says an <see cref="AiException"/> in the person's language. The result never contains the key.</summary>
public static class AiErrors
{
    public static string Describe(AiException e, string? key = null)
    {
        var p = e.ProviderName ?? "";
        var text = e.Kind switch
        {
            AiErrorKind.NeedsKey => L.F("{0} needs an API key. Add it in the AI settings.", p),
            AiErrorKind.KeyFormat => L.F("{0} needs your Account ID and API token, written as accountid:token.", p),
            AiErrorKind.Auth => L.F("{0} refused the API key. Check it in the AI settings.", p),
            AiErrorKind.NoCredit => L.F("{0} says the account has no credit left.", p),
            AiErrorKind.Quota => L.F("{0} is busy or the free quota is used up. Try again later.", p),
            AiErrorKind.BadRequest => string.IsNullOrEmpty(e.Detail) ? L.F("{0} didn't accept the request.", p)
                : L.F("{0} didn't accept the request: {1}", p, e.Detail),
            AiErrorKind.ServerError => L.F("{0} had a problem on its side. Try again later.", p),
            AiErrorKind.HttpError => L.F("{0} answered with an error ({1}).", p, e.Detail ?? ""),
            AiErrorKind.Empty => L.F("{0} sent an empty answer. Try again.", p),
            AiErrorKind.Timeout => L.F("{0} took too long to answer. Try again.", p),
            AiErrorKind.Offline => L.F("Couldn't reach {0}. Check the internet connection.", p),
            AiErrorKind.Unreadable => L.F("{0} sent an answer EasyPixel can't read. Try again.", p),
            AiErrorKind.NoPicture => L.F("{0} didn't send a picture. Try again or change the description.", p),
            AiErrorKind.BrokenPicture => L.F("{0} sent a broken picture. Try again.", p),
            AiErrorKind.Failed => L.F("{0} couldn't make this picture. Try again or change the description.", p),
            _ => L.T(e.Message),
        };
        return key is null ? text : AiKeyTester.Scrub(text, key);
    }

    /// <summary>How useful an error is to show when every generator failed: key/credit/quota problems first.</summary>
    internal static int Rank(AiErrorKind k) => k switch
    {
        AiErrorKind.Auth or AiErrorKind.KeyFormat or AiErrorKind.NeedsKey => 3,
        AiErrorKind.NoCredit or AiErrorKind.Quota => 2,
        _ => 0,
    };
}
