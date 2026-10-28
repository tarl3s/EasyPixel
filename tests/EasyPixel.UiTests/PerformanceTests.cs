using System.Diagnostics;
using Avalonia.Headless.XUnit;
using Xunit.Abstractions;
using EasyPixel.Core.Model;
using EasyPixel.Tools;

namespace EasyPixel.UiTests;

public class PerformanceTests
{
    private readonly ITestOutputHelper _out;

    public PerformanceTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// One stroke segment on a 4096 x 4096 sprite — drawing, recompositing the changed area and copying it into the
    /// canvas bitmap — must fit in a 60 fps frame. Measures the stroke + dirty-rect path, not the window paint.
    /// </summary>
    [AvaloniaFact]
    public void Canvas4096_Stroke_Under16ms()
    {
        using var h = new Harness(4096, 4096);
        var ctx = h.Window.Canvas.Context!;
        var pencil = h.Window.Tools.Get(ToolKind.Pencil);
        h.Window.Tools.SetBrush(3, Core.Raster.BrushShape.Circle);

        pencil.Down(ctx, new ToolInput(100, 100));
        for (int i = 1; i <= 5; i++) pencil.Move(ctx, new ToolInput(100 + i * 40, 100 + i * 7)); // warm-up (JIT)

        var times = new List<double>();
        var sw = new Stopwatch();
        for (int i = 0; i < 40; i++)
        {
            int x = 300 + i * 60, y = 200 + (i % 2) * 500;
            sw.Restart();
            pencil.Move(ctx, new ToolInput(x, y));
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }
        pencil.Up(ctx, new ToolInput(2700, 200));

        Assert.Equal(new Rgba(0, 0, 0, 255).Packed, h.Pixel(300, 200));
        times.Sort();
        double p90 = times[(int)(times.Count * 0.9)];
        _out.WriteLine($"segment: median {times[times.Count / 2]:F2} ms, p90 {p90:F2} ms, max {times[^1]:F2} ms");
        Assert.True(p90 < 16, $"90th percentile segment took {p90:F2} ms (median {times[times.Count / 2]:F2} ms)");
    }
}
