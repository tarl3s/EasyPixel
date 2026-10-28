using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyPixel;

/// <summary>The app's settings, kept in <c>%APPDATA%\EasyPixel\settings.json</c> (tests point <see cref="Folder"/> elsewhere).</summary>
public sealed class Settings
{
    public const int MaxRecent = 10;

    /// <summary>Where settings.json lives.</summary>
    public static string Folder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyPixel");

    private static string FilePath => Path.Combine(Folder, "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>"dark" or "light".</summary>
    public string Theme { get; set; } = "dark";
    /// <summary>A language code, or "auto" to follow the system.</summary>
    public string Language { get; set; } = "auto";
    public bool ShowGrid { get; set; } = true;
    /// <summary>Recently opened or saved files, newest first.</summary>
    public List<string> Recent { get; set; } = new();

    /// <summary>The saved settings, or defaults when there are none or the file is broken.</summary>
    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json) ?? new Settings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        return new Settings();
    }

    /// <summary>Writes the settings (through a temp file, so a crash never leaves half a file). Errors are ignored.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    /// <summary>Puts <paramref name="path"/> first in <see cref="Recent"/> (no duplicates, at most <see cref="MaxRecent"/>).</summary>
    public void AddRecent(string path)
    {
        var full = Path.GetFullPath(path);
        Recent.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        Recent.Insert(0, full);
        if (Recent.Count > MaxRecent) Recent.RemoveRange(MaxRecent, Recent.Count - MaxRecent);
    }
}
