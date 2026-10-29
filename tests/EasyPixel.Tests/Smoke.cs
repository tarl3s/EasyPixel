using EasyPixel.Core.Model;

namespace EasyPixel.Tests;

public class Smoke
{
    [Fact]
    public void CoreAssemblyLoads() =>
        Assert.Equal(new Version(0, 2, 0, 0), typeof(Sprite).Assembly.GetName().Version);
}
