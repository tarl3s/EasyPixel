using EasyPixel.Core.Update;

namespace EasyPixel.Tests;

public sealed class UpdateTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EP_Update_" + Guid.NewGuid().ToString("N")[..8]);

    public UpdateTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private const string Json = """
        {"tag_name":"v0.2.1","body":"New stuff","assets":[
          {"name":"other.zip","browser_download_url":"https://x/other.zip"},
          {"name":"EasyPixel-win-x64.zip","browser_download_url":"https://x/EasyPixel-win-x64.zip"}]}
        """;

    [Theory]
    [InlineData("v0.2.1", 0, 2, 1)]
    [InlineData("0.11", 0, 11, 0)]
    [InlineData(" V1.0.0 ", 1, 0, 0)]
    public void ParseVersion_ReadsTags(string tag, int major, int minor, int build) =>
        Assert.Equal(new Version(major, minor, build), UpdateLogic.ParseVersion(tag));

    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("v")]
    public void ParseVersion_RejectsNonVersions(string tag) => Assert.Null(UpdateLogic.ParseVersion(tag));

    [Fact]
    public void ParseLatest_FindsTheEasyPixelZip()
    {
        var r = UpdateLogic.ParseLatest(Json)!;
        Assert.Equal(new Version(0, 2, 1), r.Version);
        Assert.Equal("v0.2.1", r.Tag);
        Assert.Equal("New stuff", r.Notes);
        Assert.Equal("https://x/EasyPixel-win-x64.zip", r.ZipUrl);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"message":"Not Found"}""")]
    [InlineData("""{"tag_name":"v0.2.1","assets":[{"name":"other.zip","browser_download_url":"u"}]}""")]
    [InlineData("""{"tag_name":"nightly","assets":[{"name":"EasyPixel-win-x64.zip","browser_download_url":"u"}]}""")]
    public void ParseLatest_NullWhenUnusable(string json) => Assert.Null(UpdateLogic.ParseLatest(json));

    [Fact]
    public void NewerThan_ComparesWithTheRunningVersion()
    {
        var r = UpdateLogic.ParseLatest(Json);
        Assert.Same(r, UpdateLogic.NewerThan(r, new Version(0, 1, 0, 0)));
        Assert.Null(UpdateLogic.NewerThan(r, new Version(0, 2, 1, 0)));
        Assert.Null(UpdateLogic.NewerThan(r, new Version(1, 0, 0, 0)));
        Assert.Null(UpdateLogic.NewerThan(null, new Version(0, 1, 0, 0)));
    }

    [Fact]
    public void IsSourceBuild_FindsTheSolutionAbove()
    {
        var bin = Path.Combine(_dir, "src", "EasyPixel", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(bin);
        Assert.False(UpdateLogic.IsSourceBuild(bin));
        File.WriteAllText(Path.Combine(_dir, "EasyPixel.sln"), "");
        Assert.True(UpdateLogic.IsSourceBuild(bin));
    }

    [Fact]
    public void IsSourceBuild_FalseForAnInstalledFolder()
    {
        var install = Path.Combine(_dir, "Programs", "EasyPixel");
        Directory.CreateDirectory(install);
        Assert.False(UpdateLogic.IsSourceBuild(install));
    }

    [Fact]
    public void FilesToRemove_OnlyWhatTheManifestListsAndExists()
    {
        var install = Path.Combine(_dir, "app");
        Directory.CreateDirectory(Path.Combine(install, "Assets"));
        File.WriteAllText(Path.Combine(install, "EasyPixel.exe"), "x");
        File.WriteAllText(Path.Combine(install, "Assets", "a.dll"), "x");
        File.WriteAllText(Path.Combine(install, "my-sprite.aseprite"), "x"); // the owner's own file
        var outside = Path.Combine(_dir, "secret.txt");
        File.WriteAllText(outside, "x");
        var manifest = "EasyPixel.exe\r\nAssets/a.dll\r\ngone.dll\r\n\r\n../secret.txt\r\n" + outside + "\r\n";

        var remove = UpdateLogic.FilesToRemove(install, manifest).Select(p => Path.GetRelativePath(install, p).Replace('\\', '/')).ToList();

        Assert.Equal(new[] { "EasyPixel.exe", "Assets/a.dll" }, remove);
        Assert.True(File.Exists(outside));
    }

    [Fact]
    public void ManifestEntries_DropsUnsafeLines() =>
        Assert.Equal(new[] { "a.txt", "sub\\b.txt" }, UpdateLogic.ManifestEntries("a.txt\n\n/etc/passwd\nx/../../y\nC:\\win.ini\nsub\\b.txt\n"));
}
