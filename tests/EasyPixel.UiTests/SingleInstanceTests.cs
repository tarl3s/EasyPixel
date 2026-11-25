using Avalonia.Headless.XUnit;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.UiTests;

public class SingleInstanceTests
{
    [Fact]
    public async Task ASecondStart_HandsItsFilesToTheListeningOne()
    {
        var name = "EasyPixelTest-" + Guid.NewGuid().ToString("N");
        var got = new TaskCompletionSource<IReadOnlyList<string>>();
        using var stop = new CancellationTokenSource();
        SingleInstance.Listen(p => got.TrySetResult(p), stop.Token, name);
        bool sent = false;
        for (int i = 0; i < 20 && !sent; i++) { sent = SingleInstance.TrySend(new[] { @"C:\a\Área b.epx", @"C:\c.png" }, name); if (!sent) await Task.Delay(50); }
        Assert.True(sent);
        var paths = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { @"C:\a\Área b.epx", @"C:\c.png" }, paths); // accents survive
        stop.Cancel();
    }

    [Fact]
    public void NobodyListening_TheStartOpensItsOwnWindow()
    {
        Assert.False(SingleInstance.TrySend(new[] { @"C:\x.epx" }, "EasyPixelTest-nobody-" + Guid.NewGuid().ToString("N")));
    }

    [AvaloniaFact]
    public async Task HandedFiles_OpenAsTabs()
    {
        using var h = new Harness();
        var a = Harness.TempFile("a.png"); var b = Harness.TempFile("b.png");
        foreach (var p in new[] { a, b }) using (var f = File.Create(p)) new PngFormat().Write(new Sprite(4, 4), f, new List<string>());
        await h.Window.OpenHandedFilesAsync(new[] { a, b });
        Assert.Equal(new[] { a, b }, h.Window.Tabs.Select(t => t.Path));
    }
}
