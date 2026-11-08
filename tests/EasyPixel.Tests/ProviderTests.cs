using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Ai.Providers;
using Xunit;

namespace EasyPixel.Tests;

[CollectionDefinition("AiProviders", DisableParallelization = true)]
public sealed class AiProvidersCollection;

/// <summary>Uses AiProviders.UseForTests (global state), so it never runs next to another class of the collection.</summary>
[Collection("AiProviders")]
public class ProviderTests
{
    private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];
    private static readonly string PngB64 = Convert.ToBase64String(Png);
    private const string Key = "sk_secret_KEY_1234";
    private const string CfKey = "acct123abc:cftoken_SECRET_99";

    internal sealed class Fake(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public HttpRequestMessage? Last;
        public string? Body;
        public readonly List<(HttpRequestMessage Req, string? Body)> All = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            Last = req;
            Body = req.Content == null ? null : await req.Content.ReadAsStringAsync(ct);
            All.Add((req, Body));
            ct.ThrowIfCancellationRequested();
            return answer(req);
        }
    }

    internal static HttpResponseMessage Resp(HttpStatusCode code, string body, string type = "application/json") =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, type) };

    private static HttpResponseMessage Bytes(byte[] b) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(b) };

    public static TheoryData<string> ImageIds => new() { "cloudflare", "pollinations", "openai", "gemini", "retrodiffusion" };
    public static TheoryData<string> TextIds => new() { "cloudflare", "gemini", "openai" };

    private static IImageGenerator MakeImage(string id, HttpClient h) => id switch
    {
        "cloudflare" => new CloudflareImage(h),
        "pollinations" => new PollinationsImage(h),
        "openai" => new OpenAiImage(h),
        "gemini" => new GeminiImage(h),
        _ => new RetroDiffusionImage(h),
    };

    private static ITextAi MakeText(string id, HttpClient h) => id switch
    {
        "cloudflare" => new CloudflareText(h),
        "gemini" => new GeminiText(h),
        _ => new OpenAiText(h),
    };

    private static string KeyFor(string id) => id == "cloudflare" ? CfKey : Key;

    private static string OkImageBody(string id) => id switch
    {
        "cloudflare" => $"{{\"result\":{{\"image\":\"{PngB64}\"}},\"success\":true}}",
        "openai" => $"{{\"data\":[{{\"b64_json\":\"{PngB64}\"}}]}}",
        "gemini" => $"{{\"output_image\":{{\"mime_type\":\"image/png\",\"data\":\"{PngB64}\"}}}}",
        _ => $"{{\"base64_images\":[\"{PngB64}\"]}}",
    };

    private static HttpResponseMessage OkImage(string id) => id == "pollinations" ? Bytes(Png) : Resp(HttpStatusCode.OK, OkImageBody(id));

    private static string OkTextBody(string id) => id switch
    {
        "cloudflare" => "{\"result\":{\"response\":\" a wolf \"},\"success\":true}",
        "gemini" => "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\" a wolf \"}]}}]}",
        _ => "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\" a wolf \"}}]}",
    };

    [Theory, MemberData(nameof(ImageIds))]
    public async Task Image_200_returns_the_picture_bytes(string id)
    {
        var fake = new Fake(_ => OkImage(id));
        var bytes = await MakeImage(id, new HttpClient(fake)).GenerateAsync("a red slime", 42, KeyFor(id), default);
        Assert.Equal(Png, bytes);
    }

    [Fact]
    public async Task Cloudflare_image_request_is_documented_shape()
    {
        var fake = new Fake(_ => OkImage("cloudflare"));
        await new CloudflareImage(new HttpClient(fake)).GenerateAsync("a red slime", 42, CfKey, default);
        Assert.Equal(HttpMethod.Post, fake.Last!.Method);
        Assert.Equal("https://api.cloudflare.com/client/v4/accounts/acct123abc/ai/run/@cf/black-forest-labs/flux-1-schnell",
                     fake.Last.RequestUri!.ToString());
        Assert.Equal("Bearer cftoken_SECRET_99", fake.Last.Headers.Authorization!.ToString());
        var b = JsonNode.Parse(fake.Body!)!;
        Assert.Equal("a red slime", (string)b["prompt"]!);
        Assert.Null(b["seed"]); // the REST API refuses it
        Assert.InRange((int)b["steps"]!, 1, 8);
        Assert.Null(b["width"]);
        Assert.Null(b["height"]);
    }

    [Fact]
    public async Task Cloudflare_key_without_account_is_a_friendly_error()
    {
        var fake = new Fake(_ => OkImage("cloudflare"));
        var e = await Assert.ThrowsAsync<AiException>(() => new CloudflareImage(new HttpClient(fake)).GenerateAsync("x", 1, "justatoken", default));
        Assert.Contains("Account ID", e.Message);
        Assert.Null(fake.Last);
    }

    [Fact]
    public async Task Pollinations_request_is_documented_shape()
    {
        var fake = new Fake(_ => OkImage("pollinations"));
        await new PollinationsImage(new HttpClient(fake)).GenerateAsync("a red slime", 7, Key, default);
        var u = fake.Last!.RequestUri!;
        Assert.Equal(HttpMethod.Get, fake.Last.Method);
        Assert.Equal("gen.pollinations.ai", u.Host);
        Assert.Equal("/image/a%20red%20slime", u.AbsolutePath.Replace("+", "%20"));
        Assert.Contains("seed=7", u.Query);
        Assert.Contains("model=black-forest-labs/flux.1-schnell", Uri.UnescapeDataString(u.Query));
        Assert.Equal($"Bearer {Key}", fake.Last.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task OpenAi_image_request_is_documented_shape()
    {
        var fake = new Fake(_ => OkImage("openai"));
        await new OpenAiImage(new HttpClient(fake)).GenerateAsync("a red slime", 7, Key, default);
        Assert.Equal("https://api.openai.com/v1/images/generations", fake.Last!.RequestUri!.ToString());
        Assert.Equal($"Bearer {Key}", fake.Last.Headers.Authorization!.ToString());
        var b = JsonNode.Parse(fake.Body!)!;
        Assert.Equal("gpt-image-1", (string)b["model"]!);
        Assert.Equal("a red slime", (string)b["prompt"]!);
        Assert.Equal("png", (string)b["output_format"]!);
    }

    [Fact]
    public async Task Gemini_image_request_is_documented_shape()
    {
        var fake = new Fake(_ => OkImage("gemini"));
        await new GeminiImage(new HttpClient(fake)).GenerateAsync("a red slime", 7, Key, default);
        Assert.Equal(HttpMethod.Post, fake.Last!.Method);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/interactions", fake.Last.RequestUri!.ToString());
        Assert.Equal(Key, fake.Last.Headers.GetValues("x-goog-api-key").Single());
        Assert.Equal("2026-05-20", fake.Last.Headers.GetValues("Api-Revision").Single());
        Assert.DoesNotContain(Key, fake.Last.RequestUri.ToString());
        var b = JsonNode.Parse(fake.Body!)!;
        Assert.Equal("gemini-nano-banana-2.1", (string)b["model"]!);
        Assert.Equal("a red slime", (string)b["input"]!);
        Assert.Equal("image", (string)b["response_format"]!["type"]!);
        Assert.Equal("image/png", (string)b["response_format"]!["mime_type"]!);
    }

    private static Fake RdFake(int pendingPolls) => new(req =>
    {
        if (req.Method == HttpMethod.Post) return Resp(HttpStatusCode.OK, "{\"status\":\"accepted\",\"task_id\":\"t42\"}");
        return pendingPolls-- > 0
            ? Resp(HttpStatusCode.OK, "{\"status\":\"processing\"}")
            : Resp(HttpStatusCode.OK, $"{{\"status\":\"completed\",\"result\":{{\"base64_images\":[\"{PngB64}\"]}}}}");
    });

    [Fact]
    public async Task RetroDiffusion_starts_a_task_and_polls_it_with_backoff()
    {
        var fake = RdFake(2);
        var waits = new List<TimeSpan>();
        var rd = new RetroDiffusionImage(new HttpClient(fake)) { Delay = (t, _) => { waits.Add(t); return Task.CompletedTask; } };
        var bytes = await rd.GenerateAsync("a red slime", 7, Key, default);
        Assert.Equal(Png, bytes);

        var post = fake.All[0];
        Assert.Equal(HttpMethod.Post, post.Req.Method);
        Assert.Equal("https://api.retrodiffusion.ai/v2/inferences", post.Req.RequestUri!.ToString());
        Assert.Equal(Key, post.Req.Headers.GetValues("X-RD-Token").Single());
        var b = JsonNode.Parse(post.Body!)!;
        Assert.Equal("a red slime", (string)b["prompt"]!);
        Assert.Equal(7, (int)b["seed"]!);
        Assert.Equal(1, (int)b["num_images"]!);
        Assert.Equal("rd_fast__default", (string)b["prompt_style"]!);
        Assert.InRange((int)b["width"]!, 64, 384);

        Assert.Equal(4, fake.All.Count);
        Assert.All(fake.All.Skip(1), r =>
        {
            Assert.Equal(HttpMethod.Get, r.Req.Method);
            Assert.Equal("https://api.retrodiffusion.ai/v2/inferences/tasks/t42", r.Req.RequestUri!.ToString());
            Assert.Equal(Key, r.Req.Headers.GetValues("X-RD-Token").Single());
        });
        Assert.Equal(3, waits.Count);
        Assert.True(waits[1] > waits[0], "polling backs off");
    }

    [Fact]
    public async Task RetroDiffusion_failed_task_is_a_friendly_error()
    {
        var fake = new Fake(req => req.Method == HttpMethod.Post
            ? Resp(HttpStatusCode.OK, "{\"task_id\":\"t1\"}") : Resp(HttpStatusCode.OK, "{\"status\":\"failed\"}"));
        var rd = new RetroDiffusionImage(new HttpClient(fake)) { Delay = (_, _) => Task.CompletedTask };
        await Assert.ThrowsAsync<AiException>(() => rd.GenerateAsync("x", 1, Key, default));
    }

    [Fact]
    public async Task RetroDiffusion_cancel_during_polling_is_OperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        var fake = RdFake(1000);
        int polls = 0;
        var rd = new RetroDiffusionImage(new HttpClient(fake))
        {
            Delay = (t, ct) => { if (++polls == 3) cts.Cancel(); ct.ThrowIfCancellationRequested(); return Task.CompletedTask; },
        };
        var e = await Assert.ThrowsAnyAsync<Exception>(() => rd.GenerateAsync("x", 1, Key, cts.Token));
        Assert.IsAssignableFrom<OperationCanceledException>(e);
        Assert.True(fake.All.Count < 10);
    }

    [Theory, MemberData(nameof(TextIds))]
    public async Task Text_user_cancel_is_OperationCanceled_not_AiException(string id)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var fake = new Fake(_ => Resp(HttpStatusCode.OK, OkTextBody(id)));
        var e = await Assert.ThrowsAnyAsync<Exception>(() => MakeText(id, new HttpClient(fake)).AskAsync("s", "u", KeyFor(id), cts.Token));
        Assert.IsAssignableFrom<OperationCanceledException>(e);
    }

    public static TheoryData<string, int, string> BadAnswers()
    {
        var d = new TheoryData<string, int, string>();
        foreach (var id in new[] { "cloudflare", "pollinations", "openai", "gemini", "retrodiffusion" })
        {
            d.Add(id, 401, $"{{\"error\":{{\"message\":\"Invalid key {KeyFor(id)}\"}}}}");
            d.Add(id, 429, "{\"error\":\"slow down\"}");
            d.Add(id, 500, "oops");
            d.Add(id, 400, $"{{\"error\":{{\"message\":\"bad request for key {KeyFor(id)}\"}}}}");
            d.Add(id, 200, "<html><body>Cloudflare challenge</body></html>");
            d.Add(id, 200, "");
            d.Add(id, 200, "{}");
        }
        return d;
    }

    [Theory, MemberData(nameof(BadAnswers))]
    public async Task Image_failures_become_AiException_without_the_key(string id, int code, string body)
    {
        var fake = new Fake(_ => Resp((HttpStatusCode)code, body, body.StartsWith('<') ? "text/html" : "application/json"));
        var e = await Assert.ThrowsAsync<AiException>(() => MakeImage(id, new HttpClient(fake)).GenerateAsync("x", 1, KeyFor(id), default));
        Assert.False(string.IsNullOrWhiteSpace(e.Message));
        foreach (var part in KeyFor(id).Split(':').Append(KeyFor(id)))
            Assert.DoesNotContain(part, e.Message);
    }

    [Theory]
    [InlineData(401, AiErrorKind.Auth)]
    [InlineData(403, AiErrorKind.Auth)]
    [InlineData(402, AiErrorKind.NoCredit)]
    [InlineData(429, AiErrorKind.Quota)]
    [InlineData(400, AiErrorKind.BadRequest)]
    [InlineData(503, AiErrorKind.ServerError)]
    [InlineData(418, AiErrorKind.HttpError)]
    public async Task Status_codes_give_a_kind_and_the_provider_name(int code, AiErrorKind kind)
    {
        var fake = new Fake(_ => Resp((HttpStatusCode)code, "{\"error\":{\"message\":\"bad key " + Key + "\"}}", "application/json"));
        var e = await Assert.ThrowsAsync<AiException>(() => new OpenAiImage(new HttpClient(fake)).GenerateAsync("x", 1, Key, default));
        Assert.Equal(kind, e.Kind);
        Assert.False(string.IsNullOrEmpty(e.ProviderName));
        Assert.Contains(e.ProviderName!, e.Message);
        Assert.DoesNotContain(Key, e.Message);
        Assert.DoesNotContain(Key, e.Detail ?? "");
        if (kind == AiErrorKind.BadRequest) Assert.Contains("bad key", e.Detail);
    }

    [Theory, MemberData(nameof(ImageIds))]
    public async Task Missing_key_has_the_NeedsKey_kind(string id)
    {
        var e = await Assert.ThrowsAsync<AiException>(() => MakeImage(id, new HttpClient(new Fake(_ => OkImage(id)))).GenerateAsync("x", 1, null, default));
        Assert.True(e.Kind is AiErrorKind.NeedsKey or AiErrorKind.KeyFormat, e.Kind.ToString());
    }

    [Theory, MemberData(nameof(ImageIds))]
    public async Task Missing_key_is_a_friendly_error_and_sends_nothing(string id)
    {
        var fake = new Fake(_ => OkImage(id));
        var e = await Assert.ThrowsAsync<AiException>(() => MakeImage(id, new HttpClient(fake)).GenerateAsync("x", 1, null, default));
        Assert.Contains("key", e.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(fake.Last);
    }

    [Theory, MemberData(nameof(ImageIds))]
    public async Task User_cancel_is_OperationCanceled_not_AiException(string id)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var fake = new Fake(_ => OkImage(id));
        var e = await Assert.ThrowsAnyAsync<Exception>(() => MakeImage(id, new HttpClient(fake)).GenerateAsync("x", 1, KeyFor(id), cts.Token));
        Assert.IsAssignableFrom<OperationCanceledException>(e);
    }

    [Fact]
    public async Task Network_failure_is_a_friendly_error()
    {
        var fake = new Fake(_ => throw new HttpRequestException($"socket {Key}"));
        var e = await Assert.ThrowsAsync<AiException>(() => new OpenAiImage(new HttpClient(fake)).GenerateAsync("x", 1, Key, default));
        Assert.DoesNotContain(Key, e.Message);
    }

    [Theory, MemberData(nameof(TextIds))]
    public async Task Text_200_returns_the_answer(string id)
    {
        var fake = new Fake(_ => Resp(HttpStatusCode.OK, OkTextBody(id)));
        var answer = await MakeText(id, new HttpClient(fake)).AskAsync("SYS", "um lobo", KeyFor(id), default);
        Assert.Equal("a wolf", answer);
        Assert.Contains("SYS", fake.Body);
        Assert.Contains("um lobo", fake.Body);
    }

    [Fact]
    public async Task Text_requests_are_documented_shape()
    {
        var f1 = new Fake(_ => Resp(HttpStatusCode.OK, OkTextBody("cloudflare")));
        await new CloudflareText(new HttpClient(f1)).AskAsync("S", "U", CfKey, default);
        Assert.Equal("https://api.cloudflare.com/client/v4/accounts/acct123abc/ai/run/@cf/meta/llama-3.1-8b-instruct-fp8", f1.Last!.RequestUri!.ToString());
        Assert.Equal("system", (string)JsonNode.Parse(f1.Body!)!["messages"]![0]!["role"]!);

        var f2 = new Fake(_ => Resp(HttpStatusCode.OK, OkTextBody("gemini")));
        await new GeminiText(new HttpClient(f2)).AskAsync("S", "U", Key, default);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent", f2.Last!.RequestUri!.ToString());
        Assert.Equal("S", (string)JsonNode.Parse(f2.Body!)!["systemInstruction"]!["parts"]![0]!["text"]!);

        var f3 = new Fake(_ => Resp(HttpStatusCode.OK, OkTextBody("openai")));
        await new OpenAiText(new HttpClient(f3)).AskAsync("S", "U", Key, default);
        Assert.Equal("https://api.openai.com/v1/chat/completions", f3.Last!.RequestUri!.ToString());
        Assert.Equal($"Bearer {Key}", f3.Last.Headers.Authorization!.ToString());
    }

    [Theory]
    [InlineData("cloudflare", 401)]
    [InlineData("gemini", 429)]
    [InlineData("openai", 500)]
    [InlineData("openai", 200)]
    public async Task Text_failures_become_AiException_without_the_key(string id, int code)
    {
        var fake = new Fake(_ => Resp((HttpStatusCode)code, code == 200 ? "{}" : $"{{\"error\":{{\"message\":\"key {KeyFor(id)}\"}}}}"));
        var e = await Assert.ThrowsAsync<AiException>(() => MakeText(id, new HttpClient(fake)).AskAsync("s", "u", KeyFor(id), default));
        Assert.DoesNotContain(KeyFor(id), e.Message);
    }

    [Fact]
    public void Registry_lists_free_first_and_finds_by_id()
    {
        Assert.True(AiProviders.Images[0].Free);
        Assert.Equal("cloudflare", AiProviders.Images[0].Id);
        int firstPaid = AiProviders.Images.ToList().FindIndex(p => !p.Free);
        Assert.True(AiProviders.Images.Skip(firstPaid).All(p => !p.Free), "free generators must all come before paid ones");
        Assert.Equal(AiProviders.Images.Count, AiProviders.Images.Select(p => p.Id).Distinct().Count());
        Assert.NotNull(AiProviders.Image("retrodiffusion"));
        Assert.NotNull(AiProviders.Text("gemini"));
        Assert.Null(AiProviders.Image("nope"));
        Assert.All(AiProviders.Images, p => Assert.True(p.MaxParallel >= 1));
    }

    private sealed class Stub : IImageGenerator
    {
        public string Id => "stub"; public string Name => "Stub"; public bool NeedsKey => false; public bool Free => true; public int MaxParallel => 1;
        public Task<byte[]> GenerateAsync(string p, int s, string? k, CancellationToken ct) => Task.FromResult(Png);
    }

    [Fact]
    public void Registry_can_be_replaced_for_tests()
    {
        using (AiProviders.UseForTests([new Stub()]))
        {
            Assert.Single(AiProviders.Images);
            Assert.NotNull(AiProviders.Image("stub"));
            Assert.NotEmpty(AiProviders.Texts);
        }
        Assert.Null(AiProviders.Image("stub"));
    }
}
