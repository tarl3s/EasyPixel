using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using EasyPixel.Core.Update;

namespace EasyPixel.Update;

/// <summary>Everything the window needs to update itself. The app uses <see cref="GitHubUpdater"/>; tests give a fake.</summary>
public interface IUpdateService
{
    /// <summary>A build run from the source never checks.</summary>
    bool IsSourceBuild { get; }
    Version Current { get; }
    /// <summary>The latest release if it is newer than this program; null when not, or when GitHub can't be reached.</summary>
    Task<ReleaseInfo?> CheckAsync();
    /// <summary>Downloads and unpacks the release; returns the folder holding the new EasyPixel.exe.</summary>
    Task<string> DownloadAsync(ReleaseInfo release);
    /// <summary>Arranges for the files to be swapped once this program has closed.</summary>
    void ScheduleSwap(string newDir);
}

/// <summary>The updater's log, <c>%TEMP%\easypixel_update.log</c> (the swap script writes to it too).</summary>
public static class UpdateLog
{
    public static string File => Path.Combine(Path.GetTempPath(), "easypixel_update.log");

    public static void Write(string line)
    {
        try { System.IO.File.AppendAllText(File, $"{DateTime.Now:O} {line}\n"); }
        catch (Exception) { } // logging must never fail the caller
    }
}

/// <summary>
/// Updates a downloaded EasyPixel from the GitHub releases: asks GitHub for the latest release, downloads its zip to
/// %TEMP% and, after EasyPixel closes, a small hidden script removes the old version's files (only those listed in its
/// manifest), copies the new ones in and opens EasyPixel again.
/// </summary>
public sealed class GitHubUpdater : IUpdateService
{
    public bool IsSourceBuild => UpdateLogic.IsSourceBuild(AppContext.BaseDirectory);

    public Version Current => UpdateLogic.Normalize(typeof(GitHubUpdater).Assembly.GetName().Version);

    private static string InstallDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    private static string WorkDir => Path.Combine(Path.GetTempPath(), "EasyPixel_update");

    private static HttpClient Http(TimeSpan timeout)
    {
        var http = new HttpClient { Timeout = timeout };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("EasyPixel-updater");
        return http;
    }

    public async Task<ReleaseInfo?> CheckAsync()
    {
        try
        {
            using var http = Http(TimeSpan.FromSeconds(10));
            return UpdateLogic.NewerThan(UpdateLogic.ParseLatest(await http.GetStringAsync(UpdateLogic.LatestUrl)), Current);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException) { return null; }
    }

    public async Task<string> DownloadAsync(ReleaseInfo release)
    {
        if (Directory.Exists(WorkDir)) try { Directory.Delete(WorkDir, true); } catch (IOException) { }
        Directory.CreateDirectory(WorkDir);
        var zip = Path.Combine(WorkDir, UpdateLogic.AssetName);
        using (var http = Http(TimeSpan.FromMinutes(15)))
        {
            using var resp = await http.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            await using var src = await resp.Content.ReadAsStreamAsync();
            await using var dst = File.Create(zip);
            await src.CopyToAsync(dst);
        }
        var unpacked = Path.Combine(WorkDir, "new");
        ZipFile.ExtractToDirectory(zip, unpacked);
        return FindExeFolder(unpacked) ?? throw new InvalidDataException("The download doesn't contain EasyPixel.exe.");
    }

    /// <summary>The folder (the given one, or the single folder inside it) that holds EasyPixel.exe.</summary>
    internal static string? FindExeFolder(string dir)
    {
        if (File.Exists(Path.Combine(dir, "EasyPixel.exe"))) return dir;
        var subs = Directory.GetDirectories(dir);
        return subs.Length == 1 && File.Exists(Path.Combine(subs[0], "EasyPixel.exe")) ? subs[0] : null;
    }

    public void ScheduleSwap(string newDir)
    {
        if (!OperatingSystem.IsWindows()) return; // the swap script is PowerShell; other systems come later
        var install = InstallDir;
        var manifest = Path.Combine(install, UpdateLogic.ManifestName);
        // the old version's files to remove are decided here (testable); the script only does what the list says
        var remove = File.Exists(manifest) ? UpdateLogic.FilesToRemove(install, File.ReadAllText(manifest)) : new List<string>();
        Directory.CreateDirectory(WorkDir);
        var list = Path.Combine(WorkDir, "remove.txt");
        File.WriteAllLines(list, remove);
        var script = Path.Combine(WorkDir, "swap.ps1");
        File.WriteAllText(script, SwapScript); // plain ASCII; paths come in as arguments
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden", "-File", script,
                                  "-WaitPid", Environment.ProcessId.ToString(), "-NewDir", newDir, "-InstallDir", install, "-RemoveList", list })
            psi.ArgumentList.Add(a);
        Process.Start(psi);
    }

    private const string SwapScript = @"
param([int]$WaitPid, [string]$NewDir, [string]$InstallDir, [string]$RemoveList, [int]$WaitSeconds = 60)
$log = Join-Path $env:TEMP 'easypixel_update.log'
function Log($m) { [IO.File]::AppendAllText($log, ""$(Get-Date -Format o) $m`n"") }
Log ""updating $InstallDir from $NewDir""
$deadline = (Get-Date).AddSeconds($WaitSeconds)
while ($WaitPid -gt 0 -and (Get-Process -Id $WaitPid -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 100 }
if ($WaitPid -gt 0 -and (Get-Process -Id $WaitPid -ErrorAction SilentlyContinue)) {
    Log ""EasyPixel did not close within $WaitSeconds seconds; nothing was changed""
    exit 1
}
Start-Sleep -Milliseconds 500
$failed = 0
foreach ($f in @(Get-Content -LiteralPath $RemoveList)) {
    if ($f -and (Test-Path -LiteralPath $f -PathType Leaf)) {
        try { Remove-Item -LiteralPath $f -Force -ErrorAction Stop } catch { $failed++; Log ""could not remove ${f}: $_"" }
    }
}
$src = (Resolve-Path -LiteralPath $NewDir).Path.TrimEnd('\')
foreach ($file in Get-ChildItem -LiteralPath $src -Recurse -File) {
    $dest = Join-Path $InstallDir $file.FullName.Substring($src.Length + 1)
    try {
        $dir = Split-Path -Parent $dest
        if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir -ErrorAction Stop | Out-Null }
        Copy-Item -LiteralPath $file.FullName -Destination $dest -Force -ErrorAction Stop
    } catch { $failed++; Log ""could not copy ${dest}: $_"" }
}
Log ""copied ($failed problems)""
Start-Process (Join-Path $InstallDir 'EasyPixel.exe') -WorkingDirectory $InstallDir
";
}
