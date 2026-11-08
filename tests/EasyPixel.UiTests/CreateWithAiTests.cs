using Avalonia.Headless.XUnit;
using EasyPixel.Ai;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;

namespace EasyPixel.UiTests;

[Collection("AiProviders")]
public class CreateWithAiTests
{
    /// <summary>A 512² PNG: white background with a red square in the middle.</summary>
    private static byte[] ShapePng()
    {
        var img = new Image(512, 512, new Rgba(255, 255, 255, 255).Packed);
        for (int y = 128; y < 384; y++)
            for (int x = 160; x < 352; x++) img.Set(x, y, new Rgba(200, 30, 30, 255).Packed);
        return Png.Encode(img);
    }

    private sealed class FakeGen(string id = "fake", Func<int, byte[]>? make = null, int maxParallel = 4, TaskCompletionSource? hold = null, bool ignoreCancel = false) : IImageGenerator
    {
        private int _calls;
        public string Id => id; public string Name => id; public bool NeedsKey => true; public bool Free => true; public int MaxParallel => maxParallel;
        public List<string> Prompts { get; } = new();
        public List<int> Seeds { get; } = new();
        public async Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
        {
            int n;
            lock (Prompts) { Prompts.Add(prompt); Seeds.Add(seed); n = _calls++; }
            if (hold != null) await (ignoreCancel ? hold.Task : hold.Task.WaitAsync(ct));
            return (make ?? (_ => ShapePng()))(n);
        }
    }

    private sealed class FakeText(string reply) : ITextAi
    {
        public string Id => "faketext"; public bool NeedsKey => false;
        public List<string> Seen { get; } = new();
        public Task<string> AskAsync(string system, string user, string? key, CancellationToken ct) { Seen.Add(user); return Task.FromResult(reply); }
    }

    private static CreateWithAiWindow Window(AiCreateRequest start, params IImageGenerator[] gens) =>
        new(start, () => new AiCreateSession(gens, null, _ => "k"));

    private static async Task<CreateWithAiWindow> Generated(string request, AiCreateRequest? start = null, params IImageGenerator[] gens)
    {
        var w = Window(start ?? new AiCreateRequest(32, null), gens.Length > 0 ? gens : [new FakeGen()]);
        w.Show();
        w.RequestBox.Text = request;
        await w.GenerateAsync();
        return w;
    }

    [AvaloniaFact]
    public async Task CreateWithAi_Generate_FillsFourSlots()
    {
        var gen = new FakeGen();
        var w = await Generated("a red box", null, gen);
        Assert.All(w.Candidates, c => Assert.NotNull(c));
        Assert.All(w.Candidates, c => Assert.Equal(32, c!.Width));
        Assert.Equal(4, gen.Seeds.Distinct().Count());
        Assert.Contains("red box", gen.Prompts[0]);
        await w.GenerateAsync(); // 4 more: new seeds
        Assert.Equal(8, gen.Seeds.Distinct().Count());
        w.Close();
    }

    [AvaloniaFact]
    public async Task CreateWithAi_UseAsNewSprite_OpensSizedSprite()
    {
        var w = await Generated("a red box");
        w.Finish(false);
        var pick = Assert.IsType<AiPick>(w.Result);
        Assert.False(pick.AsNewLayer);

        using var h = new Harness();
        h.Dialogs.AiPicks.Enqueue(pick);
        await h.Window.CreateWithAiAsync(skipSetupCheck: true);
        Assert.Equal(32, h.Doc.Width);
        Assert.Equal(32, h.Doc.Height);
        Assert.Contains(h.Doc.Cels[(h.Doc.Layers[0], 0)].Image.Pixels, p => Rgba.FromPacked(p).A == 255);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_AddAsLayer_OneUndoStep()
    {
        var w = await Generated("a red box", new AiCreateRequest(32, null));
        Assert.True(w.CanAddLayer);
        w.Finish(true);
        using var h = new Harness(32, 32);
        int layers = h.Doc.Layers.Count;
        h.Dialogs.AiPicks.Enqueue(w.Result);
        await h.Window.CreateWithAiAsync(skipSetupCheck: true);
        Assert.Equal(layers + 1, h.Doc.Layers.Count);
        Assert.True(h.Doc.Cels.ContainsKey((h.Session.Layer, h.Session.Frame)));
        Assert.Contains(h.Doc.Cels[(h.Session.Layer, h.Session.Frame)].Image.Pixels, p => Rgba.FromPacked(p).A == 255);
        h.Window.Undo();
        Assert.Equal(layers, h.Doc.Layers.Count);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_AddAsLayer_LargerThanCanvas_ScaledDown()
    {
        var w = await Generated("a red box", new AiCreateRequest(16, null));
        w.SizeIndex = 2; // 48
        await w.GenerateAsync();
        Assert.Equal(48, w.Candidates[0]!.Width);
        w.Finish(true);
        using var h = new Harness(16, 16);
        h.Dialogs.AiPicks.Enqueue(w.Result);
        await h.Window.CreateWithAiAsync(skipSetupCheck: true);
        var cel = h.Doc.Cels[(h.Session.Layer, h.Session.Frame)];
        Assert.True(cel.Image.Width <= 16 && cel.Image.Height <= 16);
        Assert.True(cel.X >= 0 && cel.Y >= 0 && cel.X + cel.Image.Width <= 16 && cel.Y + cel.Image.Height <= 16);
        Assert.Contains(cel.Image.Pixels, p => Rgba.FromPacked(p).A == 255);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_PortugueseRequest_IsTranslatedBeforeGenerating()
    {
        var gen = new FakeGen();
        var text = new FakeText("{\"english\":\"a wolf with red eyes\"}");
        var s = new AiCreateSession([gen], text, _ => "k");
        await s.RunAsync("um lobo com olhos vermelhos", 32, SpriteView.Auto, null, CancellationToken.None);
        Assert.Equal("um lobo com olhos vermelhos", Assert.Single(text.Seen));
        Assert.All(gen.Prompts, p => Assert.Contains("a wolf with red eyes", p));
        Assert.DoesNotContain(gen.Prompts, p => p.Contains("lobo"));
    }

    [AvaloniaFact]
    public async Task CreateWithAi_FailedCandidate_ShowsError_OthersStillArrive()
    {
        var gen = new FakeGen(make: n => n == 1 ? [1, 2, 3] : ShapePng(), maxParallel: 1);
        var w = await Generated("a red box", null, gen);
        Assert.Equal(3, w.Candidates.Count(c => c != null));
        Assert.Single(w.Errors, e => e != null);
        w.Close();
    }

    [AvaloniaFact]
    public async Task CreateWithAi_FailingGenerator_HandsTheSlotToTheNext()
    {
        var bad = new FakeGen("bad", _ => throw new AiException("Rejected"));
        var good = new FakeGen("good");
        var s = new AiCreateSession([bad, good], null, _ => "k");
        var got = new List<(int, Image?, string?)>();
        s.CandidateReady += (i, img, err) => got.Add((i, img, err));
        await s.RunAsync("a box", 16, SpriteView.Auto, null, CancellationToken.None);
        Assert.Equal(4, got.Count(g => g.Item2 != null));

        var s2 = new AiCreateSession([bad], null, _ => "k");
        var errs = new List<string?>();
        s2.CandidateReady += (_, _, e) => errs.Add(e);
        await s2.RunAsync("a box", 16, SpriteView.Auto, null, CancellationToken.None);
        Assert.All(errs, e => Assert.Equal("Rejected", e));
    }

    [AvaloniaFact]
    public async Task CreateWithAi_AllFail_ShowsTheMostUsefulError()
    {
        var auth = new FakeGen("auth", _ => throw new AiException(AiErrorKind.Auth, "Alpha"));
        var slow = new FakeGen("slow", _ => throw new AiException(AiErrorKind.Timeout, "Beta"));
        var s = new AiCreateSession([auth, slow], null, _ => "k");
        var errs = new List<string?>();
        s.CandidateReady += (_, _, e) => errs.Add(e);
        await s.RunAsync("a box", 16, SpriteView.Auto, null, CancellationToken.None);
        Assert.Equal(4, errs.Count);
        Assert.All(errs, e => Assert.Equal("Alpha refused the API key. Check it in the AI settings.", e));
    }

    [AvaloniaFact]
    public async Task CreateWithAi_ParallelLimit_IsPerGenerator()
    {
        int busy = 0, peak = 0;
        byte[] Track(int _)
        {
            int now = Interlocked.Increment(ref busy);
            lock (this) peak = Math.Max(peak, now);
            Thread.Sleep(50);
            Interlocked.Decrement(ref busy);
            return [1];
        }
        // the first generator allows 1 at a time and always fails; the second allows 4 and must get them all at once
        var one = new FakeGen("one", _ => throw new AiException(AiErrorKind.Quota, "One"), maxParallel: 1);
        var four = new ThreadGen(Track, 4);
        var s = new AiCreateSession([one, four], null, _ => "k");
        await s.RunAsync("a box", 16, SpriteView.Auto, null, CancellationToken.None);
        Assert.True(peak > 1, $"peak {peak}");
    }

    private sealed class ThreadGen(Func<int, byte[]> make, int maxParallel) : IImageGenerator
    {
        public string Id => "thread"; public string Name => Id; public bool NeedsKey => false; public bool Free => true; public int MaxParallel => maxParallel;
        public Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct) => Task.Run(() => make(0), ct);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_DecodeAndConvert_RunOffTheUiThread_EventOnUiThread()
    {
        var gen = new FakeGen();
        var s = new AiCreateSession([gen], null, _ => "k");
        var onUi = new List<bool>();
        var got = 0;
        s.CandidateReady += (_, img, _) => { onUi.Add(Avalonia.Threading.Dispatcher.UIThread.CheckAccess()); if (img != null) got++; };
        await s.RunAsync("a box", 16, SpriteView.Auto, null, CancellationToken.None);
        Assert.Equal(4, got);
        Assert.All(onUi, Assert.True);
        // Avalonia decodes a bitmap off the UI thread
        var img2 = await Task.Run(() => ImageDecode.Load(ShapePng()));
        Assert.Equal(512, img2.Width);
    }

    [AvaloniaFact]
    public void AiLayer_DefaultName_IsInThePersonsLanguage_AndIndexedSaysColoursWereMapped()
    {
        try
        {
            L.Load("pt-BR");
            using var h = new Harness(16, 16, ColorMode.Indexed);
            var sprite = new Image(8, 8, new Rgba(200, 30, 30, 255).Packed);
            h.Window.AddAiLayer(sprite);
            Assert.StartsWith(L.T("Layer") + " ", h.Session.Layer.Name);
            Assert.NotEqual("Layer", L.T("Layer"));
            Assert.Equal(L.T("The AI sprite was added as a new layer. Its colours were matched to the palette."), h.Window.StatusText.Text);
        }
        finally { L.Load("en"); }
        using var h2 = new Harness();
        h2.Window.AddAiLayer(new Image(8, 8, new Rgba(200, 30, 30, 255).Packed));
        Assert.Equal("The AI sprite was added as a new layer.", h2.Window.StatusText.Text);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_Cancel_StopsAndAddsNothing()
    {
        // the generator ignores the cancel and answers afterwards: its picture must not arrive anywhere
        var hold = new TaskCompletionSource();
        var gen = new FakeGen(hold: hold, ignoreCancel: true);
        var session = new AiCreateSession([gen], null, _ => "k");
        int late = 0;
        session.CandidateReady += (_, _, _) => late++;
        var w = new CreateWithAiWindow(new AiCreateRequest(32, null), () => session);
        w.Show();
        w.RequestBox.Text = "a red box";
        var run = w.GenerateAsync();
        Harness.Pump();
        Assert.NotEmpty(gen.Prompts); // the request is under way
        w.Close();                    // = Cancel
        hold.SetResult();             // the service answers after the cancel
        await run;                    // stops quietly
        Harness.Pump();
        Assert.Equal(0, late);
        Assert.Null(w.Result);
        Assert.All(w.Candidates, c => Assert.Null(c));

        using var h = new Harness();
        int layers = h.Doc.Layers.Count;
        h.Dialogs.AiPicks.Enqueue(w.Result); // what the cancelled window answered
        await h.Window.CreateWithAiAsync(skipSetupCheck: true);
        Assert.Equal(layers, h.Doc.Layers.Count);
        Assert.False(h.Session.History.CanUndo);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_EmptyOrHugePrompt()
    {
        var gen = new FakeGen();
        var text = new FakeText("not json");
        var w = new CreateWithAiWindow(new AiCreateRequest(32, null), () => new AiCreateSession([gen], text, _ => "k"));
        w.Show();
        w.RequestBox.Text = "   ";
        Harness.Pump();
        Assert.False(w.CanGenerate);
        await w.GenerateAsync();
        Assert.Empty(gen.Prompts);

        var paste = string.Concat(Enumerable.Repeat("um lobo com olhos vermelhos ", 400)); // > 10 000 chars
        Assert.True(paste.Length >= 10_000);
        w.RequestBox.Text = paste;
        Harness.Pump();
        Assert.True(w.CanGenerate);
        await w.GenerateAsync();
        var sent = Assert.Single(text.Seen);
        Assert.True(sent.Length <= CreateWithAiWindow.MaxRequest);
        Assert.StartsWith(sent, paste);
        Assert.Equal(4, gen.Prompts.Count);
        Assert.All(gen.Prompts, p => Assert.DoesNotContain(paste[..(CreateWithAiWindow.MaxRequest + 30)], p));
        Assert.All(w.Candidates, c => Assert.NotNull(c));
        w.Close();
    }

    [AvaloniaFact]
    public async Task CreateWithAi_UnexpectedException_BecomesThatSlotsError()
    {
        var gen = new FakeGen(make: n => n == 2 ? throw new InvalidOperationException("boom") : ShapePng(), maxParallel: 1);
        var w = await Generated("a red box", null, gen);
        Assert.Equal(3, w.Candidates.Count(c => c != null));
        Assert.Single(w.Errors, e => e != null);
        Assert.DoesNotContain("drawing 4", w.Status);
        Assert.True(w.CanGenerate);
        w.Close();
    }

    [AvaloniaFact]
    public async Task CreateWithAi_SpritePalette_FewerThan3Colours_FallsBackWithNote()
    {
        var two = new[] { new Rgba(0, 0, 0, 255), new Rgba(255, 0, 0, 255) };
        var w = Window(new AiCreateRequest(32, two), new FakeGen());
        w.PaletteIndex = 1;
        Assert.Null(w.ChosenPalette());
        Assert.NotNull(w.Note);

        var four = new[] { new Rgba(0, 0, 0, 255), new Rgba(255, 0, 0, 255), new Rgba(0, 255, 0, 255), new Rgba(0, 0, 255, 255) };
        var w2 = Window(new AiCreateRequest(32, four), new FakeGen());
        w2.PaletteIndex = 1;
        Assert.Same(four, w2.ChosenPalette());
        Assert.Null(w2.Note);

        using var h = new Harness();
        h.Doc.Palette.Colors.Clear();
        h.Doc.Palette.Colors.AddRange(two);
        await h.Window.CreateWithAiAsync(skipSetupCheck: true);
        Assert.Equal(2, h.Dialogs.AiRequests[^1].SpritePalette!.Count);
    }

    [AvaloniaFact]
    public async Task CreateWithAi_NoGenerator_OffersSettings()
    {
        using var _ = AiProviders.UseForTests([new FakeGen()]);
        App.Settings.AiKeys.Clear();
        using var h = new Harness();
        h.Dialogs.ConfirmAnswers.Enqueue(false);
        await h.Window.CreateWithAiAsync();
        Assert.Single(h.Dialogs.Confirms);
        Assert.Empty(h.Dialogs.AiRequests);
    }
}
