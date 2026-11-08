using EasyPixel.Core.Ai;
using EasyPixel.Core.Model;

namespace EasyPixel.Ai;

/// <summary>
/// One "Generate" of the Create with AI window, without UI: the request in English → the sprite prompt → <see cref="Slots"/>
/// pictures (each with its own random seed, at most each generator's MaxParallel at once on that generator) → decoded → turned into pixel art.
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
        var gates = gens.Select(g => new SemaphoreSlim(Math.Max(1, g.MaxParallel))).ToArray();
        try
        {
            var tasks = Enumerable.Range(0, Slots).Select(slot => SlotAsync(slot, size, palette, gates, ct)).ToList();
            await Task.WhenAll(tasks);
        }
        finally { foreach (var g in gates) g.Dispose(); }
        ct.ThrowIfCancellationRequested();
    }

    private async Task SlotAsync(int slot, int size, IReadOnlyList<Rgba>? palette, SemaphoreSlim[] gates, CancellationToken ct)
    {
        Image? sprite = null;
        string? error = gens.Count == 0 ? L.T("No picture generator is set up.") : null;
        int errorRank = -1;
        void Fail(string message, int rank) { if (rank >= errorRank) { error = message; errorRank = rank; } }
        for (int g = 0; g < gens.Count && sprite == null; g++)
        {
            var gen = gens[g];
            await gates[g].WaitAsync(ct);
            try
            {
                var bytes = await gen.GenerateAsync(Prompt!, Random.Shared.Next(1, int.MaxValue), keyFor(gen.Id), ct);
                KeepRaw(slot, bytes);
                // decoding and converting a big picture takes a while: off the UI thread
                sprite = await Task.Run(() => PixelArtConverter.Convert(ImageDecode.Load(bytes), new ConvertOptions(size, Palette: palette)), ct);
                if (sprite == null) Fail(L.T("The picture had nothing to turn into a sprite."), 0);
            }
            catch (AiException ex) { Fail(AiErrors.Describe(ex, keyFor(gen.Id)), AiErrors.Rank(ex.Kind)); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
            {
                Fail(L.T("The AI service didn't answer. Check the internet connection and try again."), 0);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                Fail(L.T("Something went wrong while drawing. Try again."), 0);
            }
            finally { gates[g].Release(); }
        }
        if (sprite != null) error = null;
        ct.ThrowIfCancellationRequested();
        CandidateReady?.Invoke(slot, sprite, error);
    }

    /// <summary>Where the last run's pictures are kept as the generator sent them (before the pixel-art conversion).</summary>
    public static string RawFolder => Path.Combine(Path.GetTempPath(), "EasyPixel-AI");

    // The generator's own picture, for judging where a bad sprite goes wrong (the model or the conversion). Each run
    // overwrites the last; a failure to write is ignored.
    private static void KeepRaw(int slot, byte[] bytes)
    {
        try
        {
            Directory.CreateDirectory(RawFolder);
            bool png = bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == (byte)'P';
            foreach (var old in Directory.GetFiles(RawFolder, $"candidate{slot + 1}.*")) File.Delete(old);
            File.WriteAllBytes(Path.Combine(RawFolder, $"candidate{slot + 1}{(png ? ".png" : ".jpg")}"), bytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
