using EasyPixel.Core.Ai;
using EasyPixel.Core.Model;

namespace EasyPixel.Ai;

/// <summary>
/// One "Generate" of the Create with AI window, without UI: the request in English → the sprite prompt → <see cref="Slots"/>
/// pictures (each with its own random seed, at most the generator's MaxParallel at once) → decoded → turned into pixel art.
/// A generator that fails hands the slot to the next one; only when all fail is the slot's error reported.
/// </summary>
public sealed class AiCreateSession(IReadOnlyList<IImageGenerator> gens, ITextAi? text, Func<string, string?> keyFor)
{
    public const int Slots = 4;

    /// <summary>A slot finished: its sprite, or a friendly (already translated) error. Raised on the caller's context.</summary>
    public event Action<int, Image?, string?>? CandidateReady;

    /// <summary>The English request the last run used (after translation).</summary>
    public string? English { get; private set; }

    /// <summary>The prompt sent to the generators by the last run.</summary>
    public string? Prompt { get; private set; }

    /// <summary>Throws OperationCanceledException when <paramref name="ct"/> is cancelled.</summary>
    public async Task RunAsync(string request, int size, SpriteView view, IReadOnlyList<Rgba>? palette, CancellationToken ct)
    {
        var english = text is null ? request
            : await AiTranslate.ToEnglishAsync(request, (sys, user, c) => text.AskAsync(sys, user, keyFor(text.Id), c), ct);
        English = english;
        Prompt = SpritePrompt.Build(english, SpritePrompt.ResolveView(english, view));
        using var gate = new SemaphoreSlim(Math.Max(1, gens.Count > 0 ? gens[0].MaxParallel : 1));
        var tasks = Enumerable.Range(0, Slots).Select(slot => SlotAsync(slot, size, palette, gate, ct)).ToList();
        await Task.WhenAll(tasks);
        ct.ThrowIfCancellationRequested();
    }

    private async Task SlotAsync(int slot, int size, IReadOnlyList<Rgba>? palette, SemaphoreSlim gate, CancellationToken ct)
    {
        Image? sprite = null;
        string? error = gens.Count == 0 ? L.T("No picture generator is set up.") : null;
        await gate.WaitAsync(ct);
        try
        {
            foreach (var gen in gens)
            {
                try
                {
                    var bytes = await gen.GenerateAsync(Prompt!, Random.Shared.Next(1, int.MaxValue), keyFor(gen.Id), ct);
                    var big = ImageDecode.Load(bytes);
                    sprite = PixelArtConverter.Convert(big, new ConvertOptions(size, Palette: palette));
                    if (sprite != null) { error = null; break; }
                    error = L.T("The picture had nothing to turn into a sprite.");
                }
                catch (AiException ex) { error = L.T(ex.Message); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
                {
                    error = L.T("The AI service didn't answer. Check the internet connection and try again.");
                }
            }
        }
        finally { gate.Release(); }
        ct.ThrowIfCancellationRequested();
        CandidateReady?.Invoke(slot, sprite, error);
    }
}
