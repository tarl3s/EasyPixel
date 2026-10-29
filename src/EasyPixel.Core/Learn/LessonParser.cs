using System.Text;
using System.Text.RegularExpressions;

namespace EasyPixel.Core.Learn;

/// <summary>
/// Reads the lesson markup (see AGENTS.md, "Lessons"). Tolerant: it never throws; anything it can't use is skipped and
/// noted in <see cref="Lesson.Warnings"/>.
/// <code>
/// # Title
/// group: fundamentals            (or styles; metadata lines come before the first section)
/// summary: one line for the lesson list
///
/// ## A section
/// A paragraph: lines join until a blank line. **Bold** works inline.
/// [[example:jaggies-bad]] Caption under the picture
/// [[example:jaggies-good|Another caption]]
/// > A tip.
/// ! A common mistake.
/// - a list item
///
/// ## Try it
/// [[exercise:clean-lines]]
/// The goal, as paragraphs.
/// - [ ] a self-check item the person ticks
/// &lt;!-- a comment --&gt;
/// </code>
/// </summary>
public static partial class LessonParser
{
    [GeneratedRegex(@"^\[\[\s*(\w+)\s*:\s*([^\]|]*?)\s*(?:\|\s*([^\]]*?)\s*)?\]\]\s*(.*)$")]
    private static partial Regex MarkerRx();

    [GeneratedRegex(@"^(?:[-*+]|\d+[.)])\s+(.*)$")]
    private static partial Regex ListRx();

    [GeneratedRegex(@"^[-*+]?\s*\[\s*[xX ]?\s*\]\s*(.*)$")]
    private static partial Regex CheckRx();

    [GeneratedRegex(@"^([A-Za-z]+)\s*:\s*(.*)$")]
    private static partial Regex MetaRx();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex NumberRx();

    public static Lesson Parse(string id, string? text)
    {
        var p = new State();
        text ??= "";
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool inComment = false;
        foreach (var raw in lines)
        {
            var line = raw.Replace('\t', ' ');
            var t = line.Trim();
            if (inComment)
            {
                if (t.Contains("-->")) inComment = false;
                continue;
            }
            if (t.StartsWith("<!--"))
            {
                if (!t.Contains("-->")) inComment = true;
                continue;
            }
            p.Line(line, t);
        }
        p.Flush();
        var m = NumberRx().Match(id);
        return new Lesson
        {
            Id = id,
            Number = m.Success && int.TryParse(m.Value, out int n) ? n : 0,
            Title = string.IsNullOrWhiteSpace(p.Title) ? id : p.Title!,
            Summary = p.Summary,
            Group = p.Group,
            Blocks = p.Blocks,
            Warnings = p.Warnings,
        };
    }

    private sealed class State
    {
        public string? Title;
        public string Summary = "";
        public LessonGroup Group;
        public readonly List<LessonBlock> Blocks = new();
        public readonly List<string> Warnings = new();

        private enum Open { None, Paragraph, List, Tip, Mistake }
        private Open _open;
        private readonly StringBuilder _text = new();
        private readonly List<string> _items = new();
        private bool _ordered;
        private bool _started; // a section or a text block began: metadata lines are text from now on
        // The "Try it" being read (from [[exercise:…]] until the next heading).
        private string? _exercise;
        private readonly List<string> _goal = new();
        private readonly List<string> _checks = new();
        private int _tryItIndex = -1;

        public void Line(string line, string t)
        {
            if (t.Length == 0) { Close(); return; }

            if (t.StartsWith('#'))
            {
                int level = t.TakeWhile(c => c == '#').Count();
                var h = t[level..].Trim();
                Close();
                EndTryIt();
                if (level == 1 && Title == null) { Title = h; return; }
                _started = true;
                if (h.Length > 0) Blocks.Add(new HeadingBlock(h));
                return;
            }

            var marker = MarkerRx().Match(t);
            if (marker.Success)
            {
                Close();
                _started = true;
                var kind = marker.Groups[1].Value.ToLowerInvariant();
                var value = marker.Groups[2].Value.Trim();
                var caption = (marker.Groups[3].Success && marker.Groups[3].Value.Length > 0 ? marker.Groups[3].Value : marker.Groups[4].Value).Trim();
                if (value.Length == 0) { Warnings.Add($"Empty marker: {t}"); return; }
                if (kind == "example")
                {
                    var ex = new ExampleRef(value, caption);
                    if (Blocks.Count > 0 && Blocks[^1] is ExampleBlock prev && _exercise == null)
                        Blocks[^1] = new ExampleBlock(prev.Examples.Append(ex).ToList());
                    else Blocks.Add(new ExampleBlock(new List<ExampleRef> { ex }));
                }
                else if (kind == "exercise")
                {
                    EndTryIt();
                    _exercise = value;
                    _tryItIndex = Blocks.Count;
                    Blocks.Add(new TryItBlock(value, Array.Empty<string>(), Array.Empty<string>()));
                    if (caption.Length > 0) _goal.Add(caption);
                }
                else Warnings.Add($"Unknown marker: {t}");
                return;
            }

            if (!_started && Title != null && MetaRx().Match(t) is { Success: true } meta)
            {
                var key = meta.Groups[1].Value.ToLowerInvariant();
                var value = meta.Groups[2].Value.Trim();
                if (key == "group")
                {
                    if (value.Equals("styles", StringComparison.OrdinalIgnoreCase)) Group = LessonGroup.Styles;
                    else if (value.Equals("fundamentals", StringComparison.OrdinalIgnoreCase)) Group = LessonGroup.Fundamentals;
                    else Warnings.Add($"Unknown group: {value}");
                    return;
                }
                if (key == "summary") { Summary = value; return; }
                // Anything else ("Note: …") is ordinary text.
            }
            _started = true;

            if (_exercise != null && CheckRx().Match(t) is { Success: true } check)
            {
                Close();
                if (check.Groups[1].Value.Trim().Length > 0) _checks.Add(check.Groups[1].Value.Trim());
                return;
            }

            if (t.StartsWith('>'))
            {
                Continue(Open.Tip, t[1..].Trim());
                return;
            }
            if (t.StartsWith('!') && !t.StartsWith("!["))
            {
                Continue(Open.Mistake, t[1..].Trim());
                return;
            }
            var item = ListRx().Match(t);
            if (item.Success)
            {
                if (_open != Open.List)
                {
                    Close();
                    _ordered = char.IsDigit(t[0]);
                }
                _open = Open.List;
                _items.Add(item.Groups[1].Value.Trim());
                return;
            }
            if (_open == Open.List && line.StartsWith(' ') && _items.Count > 0)
            {
                _items[^1] += " " + t; // an indented line continues the item above
                return;
            }
            // An indented line continues a tip or mistake; anything else is paragraph text.
            Continue(_open is Open.Tip or Open.Mistake && line.StartsWith(' ') ? _open : Open.Paragraph, t);
        }

        private void Continue(Open kind, string text)
        {
            if (_open != kind) Close();
            _open = kind;
            if (_text.Length > 0) _text.Append(' ');
            _text.Append(text);
        }

        /// <summary>Ends the open paragraph, list, tip or mistake.</summary>
        private void Close()
        {
            var text = _text.ToString().Trim();
            switch (_open)
            {
                case Open.Paragraph when text.Length > 0:
                    if (_exercise != null) _goal.Add(text); else Blocks.Add(new ParagraphBlock(text));
                    break;
                case Open.Tip when text.Length > 0: Blocks.Add(new TipBlock(text)); break;
                case Open.Mistake when text.Length > 0: Blocks.Add(new MistakeBlock(text)); break;
                case Open.List when _items.Count > 0:
                    if (_exercise != null) _goal.AddRange(_items); else Blocks.Add(new ListBlock(_items.ToList(), _ordered));
                    break;
            }
            _text.Clear();
            _items.Clear();
            _open = Open.None;
        }

        private void EndTryIt()
        {
            if (_exercise == null) return;
            Close();
            Blocks[_tryItIndex] = new TryItBlock(_exercise, _goal.ToList(), _checks.ToList());
            _exercise = null;
            _goal.Clear();
            _checks.Clear();
        }

        public void Flush()
        {
            Close();
            EndTryIt();
        }
    }
}
