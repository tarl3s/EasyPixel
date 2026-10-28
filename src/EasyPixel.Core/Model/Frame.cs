namespace EasyPixel.Core.Model;

public sealed class Frame
{
    public int DurationMs { get; set; } = 100;
    /// <summary>Format-specific data EasyPixel does not model, written back by the format that read it.</summary>
    public Dictionary<string, byte[]> Extra { get; } = new();
}
