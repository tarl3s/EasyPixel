using EasyPixel.Core.Bridge;
using Microsoft.Win32;

namespace EasyPixel;

/// <summary>
/// The Windows half of <see cref="IInstallRegistry"/>: <c>HKCU\Software\&lt;app&gt;\InstallPath</c>. The only class that
/// touches the registry; on another platform it reads nothing and writes nothing.
/// </summary>
internal sealed class WindowsInstallRegistry : IInstallRegistry
{
    private const string Value = "InstallPath";

    public string? Read(string app)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\" + app);
        return key?.GetValue(Value) as string;
    }

    public void Write(string app, string path)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\" + app);
        key.SetValue(Value, path);
    }
}
