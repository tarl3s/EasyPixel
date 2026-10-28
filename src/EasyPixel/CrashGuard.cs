using Avalonia.Threading;
using EasyPixel.Core.Formats;

namespace EasyPixel;

/// <summary>
/// The last line of defence against a bug: an exception nobody caught is written to <c>easypixel_crash.log</c> in
/// <see cref="Folder"/>; an unsaved document is copied next to it as <c>easypixel_recovery_*.epx</c>. On the UI thread the
/// exception is then marked handled and the person is told, so the editor (and their work) stays open.
/// </summary>
public static class CrashGuard
{
    private static bool _installed;

    /// <summary>Where the log and the recovery copies go (the temp folder; tests use their own).</summary>
    public static string Folder { get; set; } = Path.GetTempPath();

    public static string LogPath => Path.Combine(Folder, "easypixel_crash.log");

    /// <summary>The window whose document is saved as a recovery copy and which shows the message.</summary>
    internal static MainWindow? Window { get; set; }

    /// <summary>Hooks the handlers; safe to call again (a dispatcher reset, as between UI tests, drops them).</summary>
    public static void Install()
    {
        Dispatcher.UIThread.UnhandledException -= OnUiException;
        Dispatcher.UIThread.UnhandledException += OnUiException;
        if (_installed) return;
        _installed = true;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { if (e.ExceptionObject is Exception ex) Record(ex); };
    }

    /// <summary>Unhooks the UI-thread handler (tests, so later failures aren't swallowed).</summary>
    internal static void UninstallUiHandler() => Dispatcher.UIThread.UnhandledException -= OnUiException;

    private static void OnUiException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var recovery = Record(e.Exception);
        if (Window is not { } w) return;
        var msg = L.F("Something went wrong and was stopped: {0}", e.Exception.Message);
        if (recovery != null) msg += "\n\n" + L.F("A copy of your sprite was saved to {0}", recovery);
        _ = w.Dialogs.Tell(msg);
    }

    /// <summary>Logs the exception and saves a recovery copy of a document with unsaved changes. Returns that copy's path, or null.</summary>
    internal static string? Record(Exception ex)
    {
        string? recovery = null;
        try
        {
            Directory.CreateDirectory(Folder);
            if (Window?.Session is { } s && s.History.IsDirty)
            {
                recovery = Path.Combine(Folder, $"easypixel_recovery_{DateTime.Now:yyyyMMdd_HHmmss}.epx");
                using var f = File.Create(recovery);
                new EpxFormat().Write(s.Doc, f, new List<string>());
            }
        }
        catch (Exception) { recovery = null; } // a failing copy must not hide the first error
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:O}] EasyPixel {typeof(CrashGuard).Assembly.GetName().Version}\n{ex}\n"
                + (recovery != null ? "Recovery copy: " + recovery + "\n" : "") + "\n");
        }
        catch (Exception) { }
        return recovery;
    }
}
