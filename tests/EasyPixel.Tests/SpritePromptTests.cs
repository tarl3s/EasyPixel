using EasyPixel.Core.Ai;

namespace EasyPixel.Tests;

public class SpritePromptTests
{
    [Fact] public void ResolveView_DeerMonsterIsSide() => Assert.Equal(SpriteView.Side, SpritePrompt.ResolveView("a deer-like monster walking on two legs", SpriteView.Auto));
    [Fact] public void ResolveView_ChestIsFront() => Assert.Equal(SpriteView.Front, SpritePrompt.ResolveView("a wooden chest", SpriteView.Auto));
    [Fact] public void ResolveView_ExplicitChoiceWins() => Assert.Equal(SpriteView.TopDown, SpritePrompt.ResolveView("a wooden chest", SpriteView.TopDown));

    [Fact]
    public void Build_HasRequestAndStyle()
    {
        var p = SpritePrompt.Build("a green slime", SpriteView.Side);
        Assert.StartsWith("a green slime", p);
        Assert.Contains("pixel art", p);
        Assert.Contains("side view", p);
    }

    [Fact]
    public void Build_CutsHugeRequest()
    {
        var p = SpritePrompt.Build(new string('x', 10000), SpriteView.Side);
        Assert.Contains(new string('x', 1000), p);
        Assert.DoesNotContain(new string('x', 1001), p);
    }

    [Fact]
    public void LooksEnglish_PortugueseDeerIsFalse() => Assert.False(AiTranslate.LooksEnglish(
        "faz um monstro parecido com um cervo, que anda em duas patas, as traseiras; ele tem a boca suja de sangue, olho arregalado e parece estar sempre procurando algo para atacar"));

    [Fact] public void LooksEnglish_EnglishIsTrue() => Assert.True(AiTranslate.LooksEnglish("make a wolf with red eyes"));
    [Fact] public void Clean_RejectsLostNumber() => Assert.Null(AiTranslate.Clean("um lobo com 3 olhos", "a wolf with eyes"));
    [Fact] public void Clean_StripsChatter() => Assert.Equal("a wolf", AiTranslate.Clean("um lobo", "Here is the translation: \"a wolf\""));
    [Fact] public void Clean_ReadsJson() => Assert.Equal("a wolf", AiTranslate.Clean("um lobo", "{\"english\":\"a wolf\"}"));

    [Fact]
    public async Task ToEnglish_FailingAskReturnsOriginal()
    {
        var r = await AiTranslate.ToEnglishAsync("um lobo com dois olhos", (_, _, _) => throw new InvalidOperationException(), CancellationToken.None);
        Assert.Equal("um lobo com dois olhos", r);
    }

    [Fact]
    public async Task ToEnglish_UsesTranslation()
    {
        var r = await AiTranslate.ToEnglishAsync("um lobo com dois olhos", (_, _, _) => Task.FromResult("a wolf with two eyes"), CancellationToken.None);
        Assert.Equal("a wolf with two eyes", r);
    }
}
