using System.Net;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

/// <summary>Tuning after the owner's first real run: refusals, the prompt's shape, and what is kept around the subject.</summary>
[Collection("AiProviders")]
public class AiQualityTuningTests
{
    [Fact]
    public void Soften_SwapsTheWordsGeneratorsRefuse()
    {
        var s = SpritePrompt.Soften("a purple monster with blood-stained teeth, bloody claws and blood on its fur");
        Assert.DoesNotContain("blood", s, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("red-stained teeth", s);
        Assert.Contains("purple monster", s);
    }

    [Fact]
    public void Build_PutsTheSubjectFirst_AndAsksForNothingElse()
    {
        var p = SpritePrompt.Build("a furry humanoid purple monster", SpriteView.Side);
        Assert.StartsWith("Pixel art game sprite of a furry humanoid purple monster.", p);
        Assert.Contains("side view", p);
        Assert.Contains("no shadow", p);
    }

    [Fact]
    public async Task Cloudflare_NsfwRefusal_IsItsOwnKind()
    {
        var fake = new ProviderTests.Fake(_ => ProviderTests.Resp(HttpStatusCode.BadRequest,
            "{\"errors\":[{\"message\":\"AiError: AiError: Input prompt contains NSFW content. (8d57b504-d052-4890)\"}]}", "application/json"));
        var e = await Assert.ThrowsAsync<AiException>(() =>
            new EasyPixel.Core.Ai.Providers.CloudflareImage(new HttpClient(fake)).GenerateAsync("x", 1, "acct123abc:tok_SECRET_1", default));
        Assert.Equal(AiErrorKind.Refused, e.Kind);
    }

    [Fact]
    public void Convert_DropsAPuddleBesideTheSubject()
    {
        var big = new Image(400, 400, new Rgba(255, 255, 255, 255).Packed);
        uint body = new Rgba(90, 40, 140, 255).Packed, puddle = new Rgba(150, 20, 20, 255).Packed;
        for (int y = 60; y < 340; y++) for (int x = 120; x < 280; x++) big.Set(x, y, body);   // the monster
        for (int y = 360; y < 380; y++) for (int x = 20; x < 100; x++) big.Set(x, y, puddle); // ~4% of it, apart
        var sprite = PixelArtConverter.Convert(big, new ConvertOptions(32))!;
        Assert.DoesNotContain(sprite.Pixels, p => { var c = Rgba.FromPacked(p); return c.A > 0 && c.R > 120 && c.B < 60; });
    }
}
