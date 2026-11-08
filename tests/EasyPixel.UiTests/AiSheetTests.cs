using Avalonia.Headless.XUnit;
using EasyPixel.Ai;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

[Collection("AiProviders")]
public class AiSheetTests
{
    private sealed class Gen : IImageGenerator
    {
        public string Id => "fake"; public string Name => "fake"; public bool NeedsKey => false; public bool Free => true; public int MaxParallel => 4;
        public Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct)
        {
            var img = new Image(256, 256, new Rgba(255, 255, 255, 255).Packed);
            for (int y = 40; y < 216; y++) for (int x = 70; x < 186; x++) img.Set(x, y, new Rgba(200, 30, 30, 255).Packed);
            return Task.FromResult(Png.Encode(img));
        }
    }

    [AvaloniaFact]
    public async Task Sheet_HasARowPerPrompt_AndReport()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sheet_{Guid.NewGuid():N}.png");
        try
        {
            Assert.Equal(12, AiSheet.Prompts.Length);
            Assert.Equal(0, await AiSheet.RunAsync(path, [new Gen()], null, _ => null));
            var sheet = Png.Decode(File.ReadAllBytes(path));
            Assert.True(sheet.Height % 12 == 0 && sheet.Height > 0);
            var lines = File.ReadAllLines(path + ".txt");
            var cands = lines.Where(l => l.Contains("px #")).ToList();
            Assert.Equal(12 * 2 * 4, cands.Count);
            Assert.All(cands, l => Assert.Contains("binary alpha", l));
            Assert.Equal(12, lines.Count(l => l.Contains(" prompt: ")));
        }
        finally { File.Delete(path); File.Delete(path + ".txt"); }
    }

    [AvaloniaFact]
    public async Task Sheet_NoGenerator_ExitsNonZero() =>
        Assert.Equal(2, await AiSheet.RunAsync("x.png", [], null, _ => null, log: new StringWriter()));
}
