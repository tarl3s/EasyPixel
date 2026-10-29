using System.Text.Json;

namespace EasyPixel.Core.Update;

/// <summary>A GitHub release EasyPixel can install: its version, tag, notes and the download address of the zip.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string Notes, string ZipUrl);

/// <summary>
/// The pure parts of updating (no network, no UI, nothing Windows-specific): reading GitHub's "latest release" answer,
/// comparing versions, telling a source build from an installed one and choosing which old files an update removes.
/// </summary>
public static class UpdateLogic
{
    public const string Repo = "tarl3s/EasyPixel";
    public const string AssetName = "EasyPixel-win-x64.zip";
    /// <summary>Every file of a release, one relative path per line; installed next to the program.</summary>
    public const string ManifestName = "easypixel-files.txt";
    public const string LatestUrl = "https://api.github.com/repos/" + Repo + "/releases/latest";

    /// <summary>"v0.2.0" / "0.2" gives a 3-part version; null when the text isn't one.</summary>
    public static Version? ParseVersion(string? tag)
    {
        var s = (tag ?? "").Trim().TrimStart('v', 'V');
        if (!Version.TryParse(s, out var v)) return null;
        return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
    }

    /// <summary>This program's version as 3 parts (0.1.0.0 gives 0.1.0).</summary>
    public static Version Normalize(Version? v) =>
        v == null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(0, v.Build));

    /// <summary>Reads GitHub's "latest release" answer; null when it has no version tag or no EasyPixel zip, or isn't JSON.</summary>
    public static ReleaseInfo? ParseLatest(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var tag = root.TryGetProperty("tag_name", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "";
            var version = ParseVersion(tag);
            if (version == null || !root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return null;
            foreach (var a in assets.EnumerateArray())
            {
                if (a.ValueKind != JsonValueKind.Object || !a.TryGetProperty("name", out var n) || n.GetString() != AssetName) continue;
                if (!a.TryGetProperty("browser_download_url", out var url) || url.ValueKind != JsonValueKind.String) return null;
                var notes = root.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() ?? "" : "";
                return new ReleaseInfo(version, tag, notes, url.GetString() ?? "");
            }
            return null;
        }
        catch (JsonException) { return null; }
    }

    /// <summary>The release when it is newer than <paramref name="current"/>; otherwise null.</summary>
    public static ReleaseInfo? NewerThan(ReleaseInfo? release, Version current) =>
        release != null && release.Version > Normalize(current) ? release : null;

    /// <summary>
    /// True when <paramref name="baseDir"/> is inside a source checkout (an EasyPixel.sln within a few folders above):
    /// a build run from the source never updates itself, the code is the update.
    /// </summary>
    public static bool IsSourceBuild(string baseDir)
    {
        var d = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "EasyPixel.sln"))) return true;
        return false;
    }

    /// <summary>The manifest's lines that are safe relative paths (no blanks, no rooted paths, no "..").</summary>
    public static List<string> ManifestEntries(string manifestText)
    {
        var result = new List<string>();
        foreach (var raw in manifestText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || Path.IsPathRooted(line) || line.StartsWith('/') || line.StartsWith('\\')) continue;
            if (line.Split('/', '\\').Contains("..")) continue;
            result.Add(line);
        }
        return result;
    }

    /// <summary>
    /// The files an update removes from <paramref name="installDir"/>: exactly those listed in the installed version's
    /// manifest that exist there. Anything else in the folder (the person's own files) is never touched.
    /// </summary>
    public static List<string> FilesToRemove(string installDir, string manifestText)
    {
        var root = Path.GetFullPath(installDir);
        var inside = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar; // "C:\App" must not match "C:\App2"
        var result = new List<string>();
        foreach (var rel in ManifestEntries(manifestText))
        {
            var full = Path.GetFullPath(Path.Combine(root, rel));
            if (full.StartsWith(inside, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) result.Add(full);
        }
        return result;
    }
}
