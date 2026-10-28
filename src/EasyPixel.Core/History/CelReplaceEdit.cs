using EasyPixel.Core.Model;

namespace EasyPixel.Core.History;

/// <summary>A cel's image and position at one moment (the image is a private copy).</summary>
public sealed record CelState(Image Image, int X, int Y);

/// <summary>
/// A change that may replace or grow a cel's image or move the cel (dropping moved pixels, resizing): it stores the
/// whole image before and after plus the position. Use <see cref="PixelEdit"/> when only pixels inside the image change.
/// </summary>
public sealed class CelReplaceEdit : IEdit
{
    private readonly Cel _cel;
    private readonly CelState _before;
    private readonly CelState _after;

    private CelReplaceEdit(string name, Cel cel, CelState before, CelState after)
    {
        Name = name;
        _cel = cel;
        _before = before;
        _after = after;
    }

    public string Name { get; }
    public long Bytes => (_before.Image.Pixels.Length + _after.Image.Pixels.Length) * 4L + 64;

    /// <summary>Copies the cel's image and position; call it before changing the cel.</summary>
    public static CelState Begin(Cel cel) => new(cel.Image.Clone(), cel.X, cel.Y);

    /// <summary>Records the change from <paramref name="before"/> to the cel as it is now. The cel keeps its current image object.</summary>
    public static CelReplaceEdit Capture(Cel cel, CelState before, string name) =>
        new(name, cel, before, new CelState(cel.Image, cel.X, cel.Y));

    public void Undo(Sprite s) => Apply(_before);
    public void Redo(Sprite s) => Apply(_after);

    private void Apply(CelState st)
    {
        _cel.Image = st.Image;
        _cel.X = st.X;
        _cel.Y = st.Y;
    }
}
