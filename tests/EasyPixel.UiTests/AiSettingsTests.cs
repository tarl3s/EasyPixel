using Avalonia.Headless.XUnit;
using EasyPixel.Ai;
using EasyPixel.Core.Ai;
using EasyPixel.Dialogs;

namespace EasyPixel.UiTests;

[CollectionDefinition("AiProviders", DisableParallelization = true)]
public sealed class AiProvidersCollection;

[Collection("AiProviders")]
public class AiSettingsTests
{
    private sealed class FakeImage(string id, bool free, bool needsKey, string? failWith = null) : IImageGenerator
    {
        public string Id => id; public string Name => id; public bool NeedsKey => needsKey; public bool Free => free; public int MaxParallel => 1;
        public Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct) =>
            failWith is null ? Task.FromResult(new byte[] { 1 }) : throw new AiException(failWith.Replace("{key}", key));
    }

    private static Settings WithKeys(params (string, string)[] keys)
    {
        var s = new Settings();
        foreach (var (k, v) in keys) s.AiKeys[k] = v;
        return s;
    }

    [Fact]
    public void AiSettings_SavedAndReloaded()
    {
        var old = Settings.Folder;
        Settings.Folder = Path.Combine(TestAppBuilder.TempRoot, Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var s = WithKeys(("cloudflare", "acc:tok"), ("openai", "sk-1"));
            s.AiImage = "openai"; s.AiText = "gemini";
            s.Save();
            var back = Settings.Load();
            Assert.Equal("openai", back.AiImage);
            Assert.Equal("gemini", back.AiText);
            Assert.Equal("acc:tok", back.AiKeys["cloudflare"]);
            Assert.Equal("sk-1", back.AiKeys["openai"]);
        }
        finally { Settings.Folder = old; }
    }

    [Fact]
    public void Routing_AutoPutsKeylessFirst()
    {
        using var _ = AiProviders.UseForTests([new FakeImage("a", true, true), new FakeImage("b", true, false), new FakeImage("c", false, false)]);
        var ids = AiRouting.ImageGenerators(WithKeys(("a", "k"))).Select(p => p.Id).ToArray();
        Assert.Equal(new[] { "b", "a" }, ids);
    }

    [Fact]
    public void Routing_SkipsKeyProviderWithoutKey()
    {
        using var _ = AiProviders.UseForTests([new FakeImage("a", true, true), new FakeImage("p", false, true)]);
        Assert.Empty(AiRouting.ImageGenerators(new Settings()));
        var s = WithKeys(("p", "k")); s.AiImage = "p";
        Assert.Equal("p", Assert.Single(AiRouting.ImageGenerators(s)).Id);
    }

    [Fact]
    public void Routing_TextAutoNeedsAKey()
    {
        Assert.Null(AiRouting.TextAi(new Settings()));
        Assert.Equal("gemini", AiRouting.TextAi(WithKeys(("gemini", "k")))!.Id);
        Assert.Equal("cloudflare", AiRouting.TextAi(WithKeys(("gemini", "k"), ("cloudflare", "a:b")))!.Id);
    }

    [Fact]
    public async Task Keys_NeverInStatusOrErrors()
    {
        using var _ = AiProviders.UseForTests([new FakeImage("x", true, true, "Rejected {key} badly")]);
        var (ok, msg) = await AiKeyTester.TestAsync("x", "SECRET-KEY-123");
        Assert.False(ok);
        Assert.DoesNotContain("SECRET-KEY-123", msg);
        Assert.Contains("Rejected", msg);
    }

    [Fact]
    public void Cloudflare_HalfKey_CountsAsNoKey()
    {
        Assert.False(AiRouting.HasKey(WithKeys(("cloudflare", "acc:")), "cloudflare"));
        Assert.False(AiRouting.HasKey(WithKeys(("cloudflare", ":tok")), "cloudflare"));
        Assert.False(AiRouting.HasKey(WithKeys(("cloudflare", "justtoken")), "cloudflare"));
        Assert.True(AiRouting.HasKey(WithKeys(("cloudflare", "acc:tok")), "cloudflare"));
        Assert.True(AiRouting.HasKey(WithKeys(("openai", "sk-1")), "openai"));
    }

    [AvaloniaFact]
    public void SettingsWindow_DoesNotSaveHalfCloudflareKeys()
    {
        foreach (var half in new[] { "acc:", ":tok" })
        {
            var w = new AiSettingsWindow(new AiChoice("auto", "auto", new Dictionary<string, string> { ["cloudflare"] = half, ["openai"] = "sk-1" }), _ => { });
            Assert.False(w.Choice.Keys.ContainsKey("cloudflare"), half);
            Assert.Equal("sk-1", w.Choice.Keys["openai"]);
        }
        var ok = new AiSettingsWindow(new AiChoice("auto", "auto", new Dictionary<string, string> { ["cloudflare"] = "acc:tok" }), _ => { });
        Assert.Equal("acc:tok", ok.Choice.Keys["cloudflare"]);
    }

    [AvaloniaFact]
    public async Task Tester_TranslatesProviderErrors_WithoutTheKey()
    {
        using var _ = AiProviders.UseForTests([new KindImage("x", AiErrorKind.Auth)]);
        try
        {
            L.Load("pt-BR");
            var (ok, msg) = await AiKeyTester.TestAsync("x", "SECRET-KEY-123");
            Assert.False(ok);
            Assert.Equal(L.F("{0} refused the API key. Check it in the AI settings.", "x"), msg);
            Assert.NotEqual("x refused the API key. Check it in the AI settings.", msg);
            Assert.DoesNotContain("SECRET-KEY-123", msg);
        }
        finally { L.Load("en"); }
    }

    [AvaloniaFact]
    public void Describe_EveryKind_IsTranslated_AndScrubsTheKey()
    {
        try
        {
            L.Load("pt-BR");
            foreach (var kind in Enum.GetValues<AiErrorKind>().Where(k => k != AiErrorKind.Other))
            {
                var e = new AiException(kind, "Prov", kind is AiErrorKind.BadRequest ? "bad SECRET-KEY-123" : kind is AiErrorKind.HttpError ? "418" : null);
                var msg = AiErrors.Describe(e, "SECRET-KEY-123");
                Assert.Contains("Prov", msg);
                Assert.NotEqual(e.Message, msg);
                Assert.DoesNotContain("SECRET-KEY-123", msg);
            }
        }
        finally { L.Load("en"); }
    }

    private sealed class KindImage(string id, AiErrorKind kind) : IImageGenerator
    {
        public string Id => id; public string Name => id; public bool NeedsKey => true; public bool Free => true; public int MaxParallel => 1;
        public Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct) => throw new AiException(kind, id);
    }

    [Fact]
    public async Task Test_ReportsSuccess()
    {
        using var _ = AiProviders.UseForTests([new FakeImage("x", true, true)]);
        Assert.True((await AiKeyTester.TestAsync("x", "k")).Ok);
    }

    [AvaloniaFact]
    public async Task Menu_SavesChoice()
    {
        var old = Settings.Folder;
        Settings.Folder = Path.Combine(TestAppBuilder.TempRoot, Guid.NewGuid().ToString("N")[..8]);
        try
        {
            using var h = new Harness();
            h.Dialogs.AiChoices.Enqueue(new AiChoice("pollinations", "auto", new Dictionary<string, string> { ["pollinations"] = "sk_x" }));
            await h.Window.AiSettingsAsync();
            Assert.Equal("pollinations", App.Settings.AiImage);
            Assert.Equal("sk_x", App.Settings.AiKeys["pollinations"]);
            App.Settings.AiImage = "auto"; App.Settings.AiKeys.Clear();
        }
        finally { Settings.Folder = old; }
    }
}
