using EasyPixel.Update;

namespace EasyPixel;

// Updates: at startup the window asks GitHub (once, silently) whether a newer release exists.
public partial class MainWindow
{
    /// <summary>What checks and installs updates; tests give a fake (no network).</summary>
    internal IUpdateService Updates { get; set; } = new GitHubUpdater();

    /// <summary>
    /// Asks for a newer release (not in a source build, not when the person turned it off; a missing release or no network
    /// says nothing). When there is one: asks, downloads it, and swaps the files after EasyPixel closes. Returns true when
    /// an update is waiting for the window to close.
    /// </summary>
    internal async Task<bool> CheckForUpdatesAsync()
    {
        if (!App.Settings.CheckForUpdates || Updates.IsSourceBuild) return false;
        var release = await Updates.CheckAsync();
        if (release == null) return false;
        var question = L.F("EasyPixel {0} is available (you have {1}). Download it now? It installs when you close EasyPixel.",
            release.Version.ToString(3), Updates.Current.ToString(3));
        if (!await Dialogs.Confirm(question, L.T("Download"), L.T("Not now"))) return false;
        string newDir;
        try { newDir = await Updates.DownloadAsync(release); }
        catch (Exception e) when (e is IOException or HttpRequestException or InvalidDataException or TaskCanceledException or UnauthorizedAccessException)
        {
            await Dialogs.Tell(L.T("The update could not be downloaded. Try again later."));
            return false;
        }
        Closed += (_, _) =>
        {
            try { Updates.ScheduleSwap(newDir); }
            catch (Exception e) { UpdateLog.Write("The update could not be started: " + e); } // the window is gone: nobody to tell
        };
        await Dialogs.Tell(L.T("The update is ready. It installs when you close EasyPixel."));
        return true;
    }
}
