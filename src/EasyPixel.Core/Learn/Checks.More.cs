using EasyPixel.Core.Model;

namespace EasyPixel.Core.Learn;

// The check rules of lessons 04 and 07–18 (values, anti-aliasing, gradients, textures, motion, repetition, effects,
// tags). Built through Rules.*; every message goes through Say so it is translated (see AGENTS.md, "Lessons").

internal sealed class ContrastRule(double min, double max) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        double lo = double.MaxValue, hi = double.MinValue;
        foreach (var f in input.Frames)
            foreach (uint v in f.Pixels)
            {
                if (!Opaque(v)) continue;
                double l = Luma(v);
                lo = Math.Min(lo, l);
                hi = Math.Max(hi, l);
            }
        if (hi < 0) return Say(false, "Nothing is drawn yet: draw on the drawing layer, not on the guide.");
        int range = (int)Math.Round(hi - lo);
        if (range < min)
            return Say(false, "Your darkest and lightest colours are only {0} apart in brightness (out of 255): make the darks darker or the lights lighter (at least {1}). Imagine it in greyscale.", range, (int)min);
        if (range > max)
            return Say(false, "The brightness range is {0} (out of 255): too much contrast here. Keep the darkest and lightest colours closer (at most {1}).", range, (int)max);
        return Say(true, "Brightness range {0} out of 255: the values read well.", range);
    }
}

internal sealed class AntiAliasRule(Rgba line, Rgba background, int min) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int w = img.Width, h = img.Height;
        uint L = line.Packed, B = background.Packed;
        bool IsLine(int x, int y) => (uint)x < (uint)w && (uint)y < (uint)h && img.Get(x, y) == L;

        // A "step" is a line pixel joined to the next one diagonally: where a curve moves to the next row or column.
        var step = new bool[w * h];
        int linePixels = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!IsLine(x, y)) continue;
                linePixels++;
                foreach (int dx in new[] { -1, 1 })
                    foreach (int dy in new[] { -1, 1 })
                        if (IsLine(x + dx, y + dy) && !IsLine(x + dx, y) && !IsLine(x, y + dy)) step[y * w + x] = true;
            }
        if (linePixels == 0) return Say(false, "The line is gone: keep the curve and add the in-between colours next to it.");

        double l0 = Math.Min(Luma(L), Luma(B)), l1 = Math.Max(Luma(L), Luma(B));
        int count = 0, straight = 0;
        (int, int)? firstStraight = null;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint v = img.Get(x, y);
                if (!Opaque(v) || v == L || v == B) continue;
                count++;
                double l = Luma(v);
                if (l <= l0 + 2 || l >= l1 - 2)
                    return Say(false, "The pixel at {0}, {1} isn't an in-between colour: anti-aliasing colours sit between the line and the background in brightness.", x, y);
                bool touches = false, nearStep = false;
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
                        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1 && IsLine(nx, ny)) touches = true;
                        if (step[ny * w + nx]) nearStep = true;
                    }
                if (!touches)
                    return Say(false, "The in-between pixel at {0}, {1} doesn't touch the line: anti-aliasing sits right next to the steps of the curve.", x, y);
                if (!nearStep) { straight++; firstStraight ??= (x, y); }
            }
        if (count < min)
            return Say(false, "Only {0} anti-aliasing pixel(s): add in-between colours in the corners where the curve steps (at least {1}).", count, min);
        if (straight > Math.Max(1, count * 0.15))
            return Say(false, "{0} anti-aliasing pixel(s) sit along straight parts, e.g. at {1}, {2}: straight lines don't need it, only the steps of the curve do.",
                straight, firstStraight!.Value.Item1, firstStraight.Value.Item2);
        return Say(true, "{0} anti-aliasing pixels, all on the steps of the curve.", count);
    }
}

internal sealed class GradientRule(Toward toward) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        bool vertical = toward is Toward.Down or Toward.Up;
        int n = vertical ? img.Height : img.Width;
        const int bands = 4;
        var sum = new double[bands];
        var cnt = new int[bands];
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                uint v = img.Get(x, y);
                if (!Opaque(v)) continue;
                int pos = vertical ? y : x;
                if (toward is Toward.Up or Toward.Left) pos = n - 1 - pos;
                int b = Math.Min(bands - 1, pos * bands / n);
                sum[b] += Luma(v);
                cnt[b]++;
            }
        if (cnt.Any(c => c == 0)) return Say(false, "Fill the whole canvas from one side to the other before judging the gradient.");
        var avg = sum.Zip(cnt, (s, c) => s / c).ToArray();
        for (int i = 1; i < bands; i++)
            if (avg[i] < avg[i - 1] - 2)
                return Say(false, "There's a darker band in the middle (band {0} of 4): keep each step of the gradient lighter than the one before.", i + 1);
        if (avg[^1] - avg[0] < 24)
            return Say(false, "It doesn't get lighter from one side to the other: go from dark to light step by step, in the direction the exercise asks.");
        return Say(true, "The colours go smoothly from dark to light.");
    }
}

internal sealed class TextureRule(double min, double max) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int pairs = 0, differ = 0;
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                uint v = img.Get(x, y);
                if (!Opaque(v)) continue;
                if (OpaqueAt(img, x + 1, y)) { pairs++; if (img.Get(x + 1, y) != v) differ++; }
                if (OpaqueAt(img, x, y + 1)) { pairs++; if (img.Get(x, y + 1) != v) differ++; }
            }
        if (pairs == 0) return Say(false, "Nothing is drawn yet: draw on the drawing layer, not on the guide.");
        double r = (double)differ / pairs;
        int pct = (int)Math.Round(r * 100);
        if (r < min)
            return Say(false, "The surface is almost flat ({0}% of neighbouring pixels differ): add texture like grain, cracks, bricks or folds (at least {1}%).", pct, (int)Math.Round(min * 100));
        if (r > max)
            return Say(false, "The texture is noisy ({0}% of neighbouring pixels differ): group the pixels into clusters and lines instead of scattering them (at most {1}%).", pct, (int)Math.Round(max * 100));
        return Say(true, "Texture: {0}% of neighbouring pixels differ, enough detail without noise.", pct);
    }
}

internal sealed class BounceRule(int floorY, int minHeight) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        bool touches = false, high = false;
        for (int f = 0; f < input.Frames.Count; f++)
        {
            if (Rules.Bounds(input.Frames[f]) is not { } b) return Say(false, "Frame {0} is empty: draw the ball in every frame.", f + 1);
            if (b.Y1 > floorY + 1) return Say(false, "In frame {0} the ball sinks below the floor: its lowest pixel should touch the line, not cross it.", f + 1);
            if (b.Y1 >= floorY - 1) touches = true;
            if (b.Y1 <= floorY - minHeight) high = true;
        }
        if (!touches) return Say(false, "The ball never touches the floor: in one frame its lowest pixel should sit right on the line.");
        if (!high) return Say(false, "The ball barely leaves the floor: lift it at least {0} pixels in the highest frame.", minHeight);
        return Say(true, "The ball rises and lands on the floor.");
    }
}

internal sealed class FloorRule(int floorY) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        for (int f = 0; f < input.Frames.Count; f++)
        {
            if (Rules.Bounds(input.Frames[f]) is not { } b) return Say(false, "Frame {0} is empty: draw the character in every frame.", f + 1);
            if (b.Y1 < floorY) return Say(false, "In frame {0} the feet float {1} pixel(s) above the floor: one foot always touches the ground.", f + 1, floorY - b.Y1);
            if (b.Y1 > floorY) return Say(false, "In frame {0} the feet sink {1} pixel(s) into the floor: keep them on the line.", f + 1, b.Y1 - floorY);
        }
        return Say(true, "The feet stay on the floor in every frame.");
    }
}

internal sealed class InPlaceRule(double maxDrift) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var centres = new List<double>();
        foreach (var img in input.Frames)
        {
            double sx = 0;
            int n = 0;
            for (int y = 0; y < img.Height; y++)
                for (int x = 0; x < img.Width; x++)
                    if (Opaque(img.Get(x, y))) { sx += x; n++; }
            if (n > 0) centres.Add(sx / n);
        }
        if (centres.Count == 0) return Say(false, "Nothing is drawn yet: draw on the drawing layer, not on the guide.");
        int drift = (int)Math.Round(centres.Max() - centres.Min());
        if (drift > maxDrift)
            return Say(false, "The character slides sideways by {0} pixels across the frames: a cycle stays in place (the game moves it), so keep the body over the same spot.", drift);
        return Say(true, "It stays in place: the body is over the same spot in every frame.");
    }
}

internal sealed class BobRule(int min) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var tops = input.Frames.Select(Rules.Bounds).Where(b => b != null).Select(b => b!.Value.Y0).ToList();
        if (tops.Count < 2) return Say(false, "Bobbing needs at least two drawn frames.");
        int range = tops.Max() - tops.Min();
        if (range < min)
            return Say(false, "The top of the body stays at the same height in every frame: raise it by {0} pixel(s) in some frames so it bobs.", min);
        return Say(true, "The body bobs up and down by {0} pixel(s).", range);
    }
}

internal sealed class RepeatRule(int minCopies) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        int w = img.Width, h = img.Height;
        var seen = new bool[w * h];
        var groups = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<(int, int)>();
        var cells = new List<(int X, int Y)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint v = img.Get(x, y);
                if (seen[y * w + x] || !Opaque(v)) continue;
                // One piece of a single colour (4-connected).
                cells.Clear();
                stack.Push((x, y));
                seen[y * w + x] = true;
                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    cells.Add((cx, cy));
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h || seen[ny * w + nx] || img.Get(nx, ny) != v) continue;
                        seen[ny * w + nx] = true;
                        stack.Push((nx, ny));
                    }
                }
                int x0 = cells.Min(c => c.X), y0 = cells.Min(c => c.Y), x1 = cells.Max(c => c.X), y1 = cells.Max(c => c.Y);
                // A detail: at least 2×2, not a big area like the wall.
                if (cells.Count < 3 || x1 == x0 || y1 == y0 || cells.Count > w * h / 12) continue;
                var key = new System.Text.StringBuilder().Append(v).Append(':').Append(x1 - x0).Append('x').Append(y1 - y0).Append(':');
                foreach (var c in cells.OrderBy(c => c.Y).ThenBy(c => c.X)) key.Append(c.X - x0).Append(',').Append(c.Y - y0).Append(';');
                var k = key.ToString();
                groups[k] = groups.GetValueOrDefault(k) + 1;
            }
        int best = groups.Count == 0 ? 0 : groups.Values.Max();
        if (best >= minCopies) return Say(true, "{0} matching copies of the same detail: the repetition reads as one design.", best);
        if (best < 2) return Say(false, "No repeated details yet: draw one window (or plank, or tile) and copy it with Copy and Paste so they all match.");
        return Say(false, "Only {0} matching copies: repeat the detail at least {1} times (draw one, then copy and paste it).", best, minCopies);
    }
}

internal sealed class SpaceRule(double minShare) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        if (Rules.Bounds(input.Work) is not { } b) return Say(false, "Nothing is drawn yet: draw on the drawing layer, not on the guide.");
        double share = Math.Max((b.X1 - b.X0 + 1) / (double)input.Width, (b.Y1 - b.Y0 + 1) / (double)input.Height);
        int pct = (int)Math.Round(share * 100);
        if (share < minShare)
            return Say(false, "The drawing uses {0}% of the slot: make it bigger so it reads at 1× (at least {1}% of the width or height).", pct, (int)Math.Round(minShare * 100));
        return Say(true, "The drawing fills {0}% of the slot.", pct);
    }
}

internal sealed class FadeRule : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var counts = input.Frames.Select(CountOpaque).ToList();
        if (counts.All(c => c == 0)) return Say(false, "Nothing is drawn yet: draw on the drawing layer, not on the guide.");
        if (counts.Count < 3) return Say(false, "An effect needs at least 3 frames: it starts, grows and fades.");
        int peak = counts.Max(), at = counts.IndexOf(peak);
        if (at == 0) return Say(false, "It starts at its biggest: begin small (a spark) and grow over the first frames.");
        if (counts[^1] > peak * 0.5)
            return Say(false, "The last frame is still {0}% of the biggest one: let the effect break up and fade so it ends.", (int)Math.Round(100.0 * counts[^1] / peak));
        return Say(true, "It grows to a peak and fades away.");
    }
}

internal sealed class TagsRule(int min) : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var tags = input.Sprite.Tags;
        if (tags.Count < min)
            return Say(false, "{0} tag(s) so far: name each animation with a tag (pick its frames, then Frame → New tag from the picked frames…), at least {1}.", tags.Count, min);
        if (tags.Any(t => string.IsNullOrWhiteSpace(t.Name)))
            return Say(false, "A tag has no name: give every tag the name the game will use, like idle or walk.");
        var twice = tags.GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice != null) return Say(false, "Two tags are called \"{0}\": every animation needs its own name.", twice.Key);
        for (int f = 0; f < input.Sprite.Frames.Count; f++)
            if (!tags.Any(t => f >= Math.Min(t.From, t.To) && f <= Math.Max(t.From, t.To)))
                return Say(false, "Frame {0} isn't in any tag: every frame should belong to an animation.", f + 1);
        return Say(true, "{0} tags, and every frame belongs to one.", tags.Count);
    }
}

internal sealed class BottomRule : CheckRule
{
    public override CheckLine Check(CheckInput input)
    {
        var img = input.Work;
        for (int x = 0; x < img.Width; x++)
            if (!Opaque(img.Get(x, img.Height - 1)))
                return Say(false, "The bottom row has a gap at {0}, {1}: a background layer reaches the bottom edge all the way, so nothing shows through.", x, img.Height - 1);
        return Say(true, "The layer reaches the bottom edge all the way across.");
    }
}
