namespace EasyPixel.Core.Model;

public enum LoopDir { Forward, Reverse, PingPong }

public sealed class Tag
{
    public string Name { get; set; } = "";
    public int From { get; set; }
    public int To { get; set; }
    public LoopDir Dir { get; set; }
    public Rgba Color { get; set; }
}
