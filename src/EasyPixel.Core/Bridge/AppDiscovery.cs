namespace EasyPixel.Core.Bridge;

/// <summary>
/// Where each app records its own executable so the other can find it. The one place that touches the platform's
/// store (the Windows registry); tests use a fake.
/// </summary>
public interface IInstallRegistry
{
    /// <summary>The recorded executable path of <paramref name="app"/> ("EasyEngine" or "EasyPixel"), or null.</summary>
    string? Read(string app);

    void Write(string app, string path);
}

/// <summary>What is known about an EasyEngine install: its program (null when only its data folder was found) and whether it has a Recent list.</summary>
public sealed record EngineInfo(string? ExePath, bool HasProjects);

/// <summary>Finds the sibling apps. Never throws: a missing registry or folder just means "not installed".</summary>
public sealed class AppDiscovery
{
    private readonly IInstallRegistry _registry;
    private readonly string _appData;

    /// <param name="registry">Where the apps record their executables.</param>
    /// <param name="appData">The folder that holds <c>EasyEngine\recent.json</c> (default: the user's application data).</param>
    public AppDiscovery(IInstallRegistry registry, string? appData = null)
    {
        _registry = registry;
        _appData = appData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    /// <summary>EasyEngine's recent-projects file under <paramref name="appData"/> (default: the user's application data).</summary>
    public static string EngineRecentFile(string? appData = null) =>
        Path.Combine(appData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyEngine", "recent.json");

    /// <summary>The registered EasyEngine program if it still exists, else its data folder if there is one ("installed, path unknown"), else null.</summary>
    public EngineInfo? FindEasyEngine()
    {
        var hasProjects = SafeExists(EngineRecentFile(_appData), file: true);
        if (Registered("EasyEngine") is { } exe) return new EngineInfo(exe, hasProjects);
        if (SafeExists(Path.Combine(_appData, "EasyEngine"), file: false)) return new EngineInfo(null, hasProjects);
        return null;
    }

    /// <summary>The registered EasyPixel program if it still exists, else null.</summary>
    public string? FindEasyPixel() => Registered("EasyPixel");

    private string? Registered(string app)
    {
        try
        {
            var path = _registry.Read(app);
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }
        catch (Exception e) when (e is not OutOfMemoryException) // any platform trouble means "not registered"
        {
            return null;
        }
    }

    private static bool SafeExists(string path, bool file)
    {
        try { return file ? File.Exists(path) : Directory.Exists(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
}
