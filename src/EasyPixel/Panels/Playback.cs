using EasyPixel.Core.Model;

namespace EasyPixel.Panels;

/// <summary>
/// Which frame comes next while the animation plays. Like Aseprite, playing from inside a tag plays only that tag's
/// frames in its direction (forward, reverse, ping-pong); outside every tag the whole animation plays forward.
/// </summary>
public sealed class Playback
{
    private int _step = 1; // +1 or -1; ping-pong turns it around at the ends

    /// <summary>The frames being played and how.</summary>
    public (int From, int To, LoopDir Dir) Range { get; private set; }

    public bool Loop { get; set; } = true;

    /// <summary>The innermost (shortest) tag holding <paramref name="frame"/>, or null.</summary>
    public static Tag? TagAt(Sprite s, int frame) =>
        s.Tags.Where(t => t.From <= frame && frame <= t.To).OrderBy(t => t.To - t.From).FirstOrDefault();

    /// <summary>Starts playing from <paramref name="frame"/>: picks the range and direction.</summary>
    public void Start(Sprite s, int frame)
    {
        var tag = TagAt(s, frame);
        Range = tag == null
            ? (0, s.Frames.Count - 1, LoopDir.Forward)
            : (Math.Max(0, tag.From), Math.Min(s.Frames.Count - 1, tag.To), tag.Dir);
        _step = Range.Dir == LoopDir.Reverse ? -1 : 1;
    }

    /// <summary>The frame after <paramref name="frame"/>, or null when a non-looping animation has ended.</summary>
    public int? Next(int frame)
    {
        var (from, to, dir) = Range;
        if (from >= to) return Loop ? from : null;
        if (frame < from || frame > to) return _step > 0 ? from : to;
        int next = frame + _step;
        if (next >= from && next <= to) return next;
        if (dir == LoopDir.PingPong)
        {
            // Turn around at an end; without looping, one there-and-back is the whole animation.
            if (!Loop && _step < 0) return null;
            _step = -_step;
            return frame + _step;
        }
        if (!Loop) return null;
        return _step > 0 ? from : to;
    }
}
