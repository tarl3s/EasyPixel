using EasyPixel.Core.History;
using EasyPixel.Core.Model;
using EasyPixel.Core.Select;

namespace EasyPixel.Tools;

/// <summary>
/// Move: dragging inside the selection lifts its pixels (they float until dropped: another tool, Enter, Deselect,
/// Save…); dragging floating pixels moves them; with nothing selected the whole cel moves. Arrow keys nudge.
/// </summary>
public sealed class MoveTool : Tool
{
    private int _startX, _startY, _originX, _originY;
    private Cel? _movingCel;

    public MoveTool() : base(ToolKind.Move, "Move") { }

    public override void Down(ToolContext c, ToolInput p)
    {
        var s = c.Session;
        if (s.Float is { } fl && fl.Bounds.Contains(p.X, p.Y))
        {
            Grab(p, fl.Pixels.X, fl.Pixels.Y);
            return;
        }
        s.DropFloating();
        if (s.WhyNotEditable() is { } why) { c.Status(why); return; }
        if (!s.Sel.IsEmpty)
        {
            if (!s.Sel.Contains(p.X, p.Y)) return;
            s.LiftSelection("Move");
            Grab(p, s.Float!.Pixels.X, s.Float.Pixels.Y);
            return;
        }
        if (!s.Doc.Cels.TryGetValue((s.Layer, s.Frame), out var cel)) return;
        _movingCel = cel;
        Grab(p, cel.X, cel.Y);
    }

    public override void Move(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        int x = _originX + p.X - _startX, y = _originY + p.Y - _startY;
        var s = c.Session;
        if (_movingCel is { } cel)
        {
            var old = new PixelRect(cel.X, cel.Y, cel.Image.Width, cel.Image.Height);
            cel.X = x;
            cel.Y = y;
            s.Invalidate(old.Union(new PixelRect(x, y, cel.Image.Width, cel.Image.Height)));
        }
        else s.MoveFloating(x, y);
    }

    public override void Up(ToolContext c, ToolInput p)
    {
        if (!Active) return;
        Move(c, p);
        if (_movingCel is { } cel && (cel.X, cel.Y) != (_originX, _originY))
            c.Session.Push(new CelMoveEdit(cel, _originX, _originY, cel.X, cel.Y));
        _movingCel = null;
        Active = false;
    }

    public override void Cancel(ToolContext c)
    {
        if (_movingCel is { } cel) { cel.X = _originX; cel.Y = _originY; c.Session.InvalidateAll(); }
        _movingCel = null;
        Active = false;
    }

    /// <summary>Arrow keys: moves the floating pixels (lifting the selection first) or, with nothing selected, the cel.</summary>
    public static void Nudge(ToolContext c, int dx, int dy)
    {
        var s = c.Session;
        if (s.Float == null)
        {
            if (s.WhyNotEditable() is { } why) { c.Status(why); return; }
            if (s.Sel.IsEmpty)
            {
                if (!s.Doc.Cels.TryGetValue((s.Layer, s.Frame), out var cel)) return;
                var old = new PixelRect(cel.X, cel.Y, cel.Image.Width, cel.Image.Height);
                cel.X += dx;
                cel.Y += dy;
                s.Push(new CelMoveEdit(cel, old.X, old.Y, cel.X, cel.Y));
                s.Invalidate(old.Union(new PixelRect(cel.X, cel.Y, old.W, old.H)));
                return;
            }
            s.LiftSelection("Move");
        }
        var f = s.Float!.Pixels;
        s.MoveFloating(f.X + dx, f.Y + dy);
    }

    private void Grab(ToolInput p, int originX, int originY)
    {
        Active = true;
        _startX = p.X;
        _startY = p.Y;
        _originX = originX;
        _originY = originY;
    }
}

/// <summary>A cel moved on the canvas without changing its pixels.</summary>
internal sealed class CelMoveEdit : IEdit
{
    private readonly Cel _cel;
    private readonly int _oldX, _oldY, _newX, _newY;

    public CelMoveEdit(Cel cel, int oldX, int oldY, int newX, int newY)
    {
        _cel = cel; _oldX = oldX; _oldY = oldY; _newX = newX; _newY = newY;
    }

    public string Name => "Move";
    public long Bytes => 48;
    public void Undo(Sprite s) { _cel.X = _oldX; _cel.Y = _oldY; }
    public void Redo(Sprite s) { _cel.X = _newX; _cel.Y = _newY; }
}
