using Avalonia.Headless.XUnit;
using EasyPixel.Core.Update;
using EasyPixel.Dialogs;
using EasyPixel.Update;

namespace EasyPixel.UiTests;

public class UpdateUiTests
{
    private sealed class FakeUpdates : IUpdateService
    {
        public bool IsSourceBuild { get; init; }
        public Version Current { get; } = new(0, 1, 0);
        public ReleaseInfo? Release { get; init; }
        public int Checks, Downloads;
        public List<string> Swaps { get; } = new();
        public bool DownloadFails { get; init; }

        public Task<ReleaseInfo?> CheckAsync() { Checks++; return Task.FromResult(Release); }

        public Task<string> DownloadAsync(ReleaseInfo release)
        {
            Downloads++;
            return DownloadFails ? throw new IOException("offline") : Task.FromResult("new-dir");
        }

        public void ScheduleSwap(string newDir) => Swaps.Add(newDir);
    }

    private static readonly ReleaseInfo Newer = new(new Version(0, 2, 0), "v0.2.0", "", "https://x/z.zip");

    private static Harness Make(FakeUpdates updates, bool check = true)
    {
        var h = new Harness();
        h.Window.Updates = updates;
        App.Settings.CheckForUpdates = check;
        return h;
    }

    [AvaloniaFact]
    public async Task Update_AskedDownloadedAndSwappedWhenTheWindowCloses()
    {
        var u = new FakeUpdates { Release = Newer };
        using var h = Make(u);
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        Assert.True(await h.Window.CheckForUpdatesAsync());
        Assert.Contains("0.2.0", h.Dialogs.Confirms.Single());
        Assert.Equal(1, u.Downloads);
        Assert.Empty(u.Swaps);
        h.Window.Close();
        Harness.Pump();
        Assert.Equal(new[] { "new-dir" }, u.Swaps);
    }

    [AvaloniaFact]
    public async Task Update_DeclinedDownloadsNothing()
    {
        var u = new FakeUpdates { Release = Newer };
        using var h = Make(u);
        h.Dialogs.ConfirmAnswers.Enqueue(false);
        Assert.False(await h.Window.CheckForUpdatesAsync());
        Assert.Equal(0, u.Downloads);
    }

    [AvaloniaFact]
    public async Task Update_SourceBuildOrSettingOffNeverChecks()
    {
        var src = new FakeUpdates { IsSourceBuild = true, Release = Newer };
        using (var h = Make(src)) Assert.False(await h.Window.CheckForUpdatesAsync());
        var off = new FakeUpdates { Release = Newer };
        using (var h = Make(off, check: false)) Assert.False(await h.Window.CheckForUpdatesAsync());
        App.Settings.CheckForUpdates = true;
        Assert.Equal(0, src.Checks + off.Checks);
    }

    [AvaloniaFact]
    public async Task Update_NoNewerReleaseIsSilent()
    {
        var u = new FakeUpdates();
        using var h = Make(u);
        Assert.False(await h.Window.CheckForUpdatesAsync());
        Assert.Empty(h.Dialogs.Confirms);
        Assert.Empty(h.Dialogs.Told);
    }

    [AvaloniaFact]
    public async Task Update_FailedDownloadTellsAndSchedulesNothing()
    {
        var u = new FakeUpdates { Release = Newer, DownloadFails = true };
        using var h = Make(u);
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        Assert.False(await h.Window.CheckForUpdatesAsync());
        Assert.Single(h.Dialogs.Told);
        h.Window.Close();
        Harness.Pump();
        Assert.Empty(u.Swaps);
    }

    [AvaloniaFact]
    public async Task Settings_UpdateToggleIsSaved()
    {
        using var h = new Harness();
        h.Dialogs.SettingsChoices.Enqueue(new SettingsChoice(App.Settings.Language, App.Settings.Theme, false));
        await h.Window.SettingsAsync();
        Assert.False(App.Settings.CheckForUpdates);
        Assert.Empty(h.Dialogs.Told); // nothing needs a restart
        App.Settings.CheckForUpdates = true;
        App.Settings.Save();
    }
}
