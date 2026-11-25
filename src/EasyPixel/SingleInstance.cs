using System.IO.Pipes;

namespace EasyPixel;

/// <summary>
/// One EasyPixel window: the running one listens on a named pipe; a second start with files (a double-click on an
/// .epx, "Open with") hands their paths over and quits, and the running window opens them in tabs.
/// </summary>
internal static class SingleInstance
{
    /// <summary>One pipe per Windows user (another user's EasyPixel is theirs).</summary>
    public static string PipeName => "EasyPixel-" + Environment.UserName;

    /// <summary>Sends the files to a running EasyPixel. True when one took them; false when none is listening.</summary>
    public static bool TrySend(IReadOnlyList<string> paths, string? pipeName = null)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName ?? PipeName, PipeDirection.Out);
            pipe.Connect(300);
            using var w = new StreamWriter(pipe, new System.Text.UTF8Encoding(false));
            foreach (var p in paths) w.WriteLine(Path.GetFullPath(p));
            w.Flush();
            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>
    /// Listens until <paramref name="stop"/>: every batch of paths another start sends is given to <paramref name="onFiles"/>
    /// (on a background thread; the caller moves to the UI thread). A pipe already taken (another EasyPixel listening)
    /// is retried quietly.
    /// </summary>
    public static void Listen(Action<IReadOnlyList<string>> onFiles, CancellationToken stop, string? pipeName = null)
    {
        _ = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var pipe = new NamedPipeServerStream(pipeName ?? PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipe.WaitForConnectionAsync(stop);
                    using var r = new StreamReader(pipe, System.Text.Encoding.UTF8);
                    var paths = new List<string>();
                    while (await r.ReadLineAsync(stop) is { } line)
                        if (line.Length > 0) paths.Add(line);
                    if (paths.Count > 0) onFiles(paths);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    try { await Task.Delay(1000, stop); } catch (OperationCanceledException) { return; }
                }
            }
        });
    }
}
