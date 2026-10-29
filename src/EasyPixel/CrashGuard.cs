using Avalonia.Threading;
using EasyPixel.Core.Formats;

namespace EasyPixel;

/// <summary>
/// The last line of defence against a bug: an exception nobody caught is written to <c>easypixel_crash.log</c> in
/// <see cref="Folder"/>; an unsaved document is copied next to it as <c>easypixel_recovery_*.epx</c>. On the UI thread the
/// exception is then marked handled and the person is told, so the editor (and their work) stays open. An error that
/// keeps coming back (a timer, a redraw) is logged and told once, with at most one recovery copy per minute.
/// </summary>
public static class CrashGuard
{
    private static bool _installed;
    private static readonly object Gate = new();
    private static string? _lastKey, _lastRecovery;
    private static DateTime _lastSeen, _lastRecoveryAt;
    private static int _repeats;
    private static bool _telling;

    /// <summary>The same error again within this time is counted, not logged or told again.</summary>
    public static TimeSpan RepeatWindow { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>At most one recovery copy per this time (a copy of a 4096² sprite is big).</summary>
    public static TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The time (tests move it).</summary>
    internal static Func<DateTime> Clock { get; set; } = () => DateTime.Now;

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
        var recovery = Record(e.Exception, out bool repeat);
        // A bug that repeats (in a timer or while drawing) is told once; and never a second message over the first.
        if (repeat || _telling || Window is not { } w) return;
        var msg = L.F("Something went wrong and was stopped: {0}", e.Exception.Message);
        if (recovery != null) msg += "\n\n" + L.F("A copy of your sprite was saved to {0}", recovery);
        _ = TellAsync(w, msg);
    }

    private static async Task TellAsync(MainWindow w, string msg)
    {
        _telling = true;
        try { await w.Dialogs.Tell(msg); }
        finally { _telling = false; }
    }

    /// <summary>Logs the exception and saves a recovery copy of a document with unsaved changes. Returns that copy's path, or null.</summary>
    internal static string? Record(Exception ex) => Record(ex, out _);

    /// <summary>
    /// As <see cref="Record(Exception)"/>; <paramref name="repeat"/> is true when the same error already came within
    /// <see cref="RepeatWindow"/> (it is then counted, not logged again). A recovery copy is written at most once per
    /// <see cref="RecoveryInterval"/>; in between the last copy's path is returned.
    /// </summary>
    internal static string? Record(Exception ex, out bool repeat)
    {
        lock (Gate)
        {
            var now = Clock();
            string key = ex.GetType().FullName + "|" + ex.Message + "|" + ex.StackTrace?.Split('\n')[0];
            repeat = key == _lastKey && now - _lastSeen < RepeatWindow;
            _lastSeen = now;
            if (repeat) { _repeats++; return _lastRecovery; }
            _lastKey = key;
            int repeated = _repeats;
            _repeats = 0;

            string? recovery = null;
            try
            {
                Directory.CreateDirectory(Folder);
                if (Window?.Session is { } s && s.History.IsDirty)
                {
                    if (_lastRecovery != null && now - _lastRecoveryAt < RecoveryInterval) recovery = _lastRecovery;
                    else
                    {
                        recovery = Path.Combine(Folder, $"easypixel_recovery_{now:yyyyMMdd_HHmmss}.epx");
                        using (var f = File.Create(recovery)) new EpxFormat().Write(s.Doc, f, new List<string>());
                        _lastRecovery = recovery;
                        _lastRecoveryAt = now;
                    }
                }
            }
            catch (Exception) { recovery = null; } // a failing copy must not hide the first error
            try
            {
                File.AppendAllText(LogPath, (repeated > 0 ? $"(the previous error repeated {repeated} more times)\n\n" : "")
                    + $"[{now:O}] EasyPixel {typeof(CrashGuard).Assembly.GetName().Version}\n{ex}\n"
                    + (recovery != null ? "Recovery copy: " + recovery + "\n" : "") + "\n");
            }
            catch (Exception) { }
            return recovery;
        }
    }

    /// <summary>Forgets earlier errors and copies (tests).</summary>
    internal static void Reset()
    {
        lock (Gate)
        {
            _lastKey = null;
            _lastSeen = DateTime.MinValue;
            _repeats = 0;
            _lastRecovery = null;
            _lastRecoveryAt = DateTime.MinValue;
            _telling = false;
        }
    }
}
