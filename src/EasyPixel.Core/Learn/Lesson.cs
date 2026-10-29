namespace EasyPixel.Core.Learn;

/// <summary>Where a lesson sits in the Learn window's list.</summary>
public enum LessonGroup { Fundamentals, Styles }

/// <summary>One piece of a lesson page, in reading order.</summary>
public abstract record LessonBlock;

/// <summary>A section title (<c>## …</c>).</summary>
public sealed record HeadingBlock(string Text) : LessonBlock;

/// <summary>Plain text; may hold <c>**bold**</c> spans (see <see cref="Inline.Split"/>).</summary>
public sealed record ParagraphBlock(string Text) : LessonBlock;

/// <summary>A bulleted list (<c>- …</c>, <c>* …</c>) or, when its first item is <c>1. …</c>, a numbered one.</summary>
public sealed record ListBlock(IReadOnlyList<string> Items, bool Ordered = false) : LessonBlock;

/// <summary>A tip box (<c>&gt; …</c>).</summary>
public sealed record TipBlock(string Text) : LessonBlock;

/// <summary>A "common mistake" box (<c>! …</c>).</summary>
public sealed record MistakeBlock(string Text) : LessonBlock;

/// <summary>One illustration: a <see cref="LessonArt"/> id and an optional caption.</summary>
public sealed record ExampleRef(string Id, string Caption);

/// <summary>Illustrations shown side by side (consecutive <c>[[example:id]]</c> lines).</summary>
public sealed record ExampleBlock(IReadOnlyList<ExampleRef> Examples) : LessonBlock;

/// <summary>
/// The lesson's practice: <c>[[exercise:id]]</c> names an <see cref="Exercises"/> entry; the text after it (until the
/// next heading) is the goal, and <c>- [ ] …</c> lines are the self-check items the person ticks.
/// </summary>
public sealed record TryItBlock(string ExerciseId, IReadOnlyList<string> Goal, IReadOnlyList<string> SelfChecks) : LessonBlock;

/// <summary>A parsed lesson page.</summary>
public sealed class Lesson
{
    /// <summary>The file name without extension, e.g. <c>01-pixels-and-canvas</c>; the same in every language.</summary>
    public required string Id { get; init; }
    /// <summary>The number the file name starts with (0 when it has none).</summary>
    public int Number { get; init; }
    public required string Title { get; init; }
    public string Summary { get; init; } = "";
    public LessonGroup Group { get; init; }
    public required IReadOnlyList<LessonBlock> Blocks { get; init; }
    /// <summary>What the parser skipped or guessed (unknown markers, bad metadata). Shipped lessons have none (tested).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>The first "Try it" block, or null.</summary>
    public TryItBlock? TryIt => Blocks.OfType<TryItBlock>().FirstOrDefault();

    public IEnumerable<string> ExampleIds => Blocks.OfType<ExampleBlock>().SelectMany(b => b.Examples).Select(e => e.Id);
}

/// <summary>Inline formatting of lesson text: <c>**bold**</c> only.</summary>
public static class Inline
{
    /// <summary>Splits text into runs; an unmatched <c>**</c> stays as plain text.</summary>
    public static List<(string Text, bool Bold)> Split(string text)
    {
        var runs = new List<(string, bool)>();
        int i = 0;
        while (i < text.Length)
        {
            int open = text.IndexOf("**", i, StringComparison.Ordinal);
            int close = open < 0 ? -1 : text.IndexOf("**", open + 2, StringComparison.Ordinal);
            if (open < 0 || close < 0)
            {
                runs.Add((text[i..], false));
                break;
            }
            if (open > i) runs.Add((text[i..open], false));
            if (close > open + 2) runs.Add((text[(open + 2)..close], true));
            i = close + 2;
        }
        return runs;
    }

    /// <summary>The text without formatting marks.</summary>
    public static string Plain(string text) => string.Concat(Split(text).Select(r => r.Text));
}
