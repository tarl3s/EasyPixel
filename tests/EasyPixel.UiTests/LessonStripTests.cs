using Avalonia.Headless.XUnit;
using EasyPixel.Core.Learn;
using EasyPixel.Learn;

namespace EasyPixel.UiTests;

public class LessonStripTests
{
    /// <summary>The bar scrolls: what failed comes first, so a ✗ is never hidden below "You drew N pixels".</summary>
    [AvaloniaFact]
    public void CheckResults_ShowWhatFailedFirst()
    {
        var strip = new LessonStrip();
        strip.ShowResult(new CheckResult(new[]
        {
            new CheckLine(true, "You drew {0} pixels.", new object[] { 320 }),
            new CheckLine(false, "Swatch {0} isn't lighter than swatch {1}: a ramp goes from dark to light, one clear step at a time.", new object[] { 3, 2 }),
        }));
        Assert.StartsWith("✗ Swatch 3", strip.ResultLines[0]);
        Assert.StartsWith("✓", strip.ResultLines[1]);
    }
}
