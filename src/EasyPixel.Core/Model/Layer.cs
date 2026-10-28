namespace EasyPixel.Core.Model;

/// <summary>Same order as the ids in Aseprite files.</summary>
public enum BlendMode
{
    Normal, Multiply, Screen, Overlay, Darken, Lighten, ColorDodge, ColorBurn, HardLight, SoftLight,
    Difference, Exclusion, Hue, Saturation, Color, Luminosity, Addition, Subtract, Divide,
}

public sealed class Layer
{
    public string Name { get; set; } = "Layer";
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public byte Opacity { get; set; } = 255;
    public BlendMode Blend { get; set; }
    public Layer? Parent { get; set; }
    public bool IsGroup { get; set; }
    /// <summary>Format-specific data EasyPixel does not model, written back by the format that read it.</summary>
    public Dictionary<string, byte[]> Extra { get; } = new();
}
