using EasyPixel.Core.Learn;

namespace EasyPixel.Tests;

public class LearnParserTests
{
    private const string Sample = """
        # Lines and curves
        group: styles
        summary: Clean slopes and smooth curves.

        ## Why lines matter
        A line is a row of
        pixels, joined.

        Second **bold** paragraph.
        [[example:jaggies-bad]] Uneven steps
        [[example:jaggies-good|Even steps]]

        > A tip
        > on two lines.
        ! A mistake.
        - first item
          continues here
        * second item
        1. third item

        ## Try it
        [[exercise:clean-lines]]
        Draw three lines.
        - [ ] My slopes are even.
        [x] My curve is smooth.
        """;

    [Fact]
    public void Parse_ReadsEveryBlockKind()
    {
        var l = LessonParser.Parse("02-lines-and-curves", Sample);
        Assert.Equal("Lines and curves", l.Title);
        Assert.Equal(2, l.Number);
        Assert.Equal(LessonGroup.Styles, l.Group);
        Assert.Equal("Clean slopes and smooth curves.", l.Summary);
        Assert.Empty(l.Warnings);
        var b = l.Blocks;
        Assert.Equal(new HeadingBlock("Why lines matter"), b[0]);
        Assert.Equal(new ParagraphBlock("A line is a row of pixels, joined."), b[1]);
        Assert.Equal(new ParagraphBlock("Second **bold** paragraph."), b[2]);
        var ex = Assert.IsType<ExampleBlock>(b[3]);
        Assert.Equal(new[] { new ExampleRef("jaggies-bad", "Uneven steps"), new ExampleRef("jaggies-good", "Even steps") }, ex.Examples);
        Assert.Equal(new TipBlock("A tip on two lines."), b[4]);
        Assert.Equal(new MistakeBlock("A mistake."), b[5]);
        Assert.Equal(new[] { "first item continues here", "second item", "third item" }, Assert.IsType<ListBlock>(b[6]).Items);
        Assert.Equal(new HeadingBlock("Try it"), b[7]);
        var t = Assert.IsType<TryItBlock>(b[8]);
        Assert.Equal("clean-lines", t.ExerciseId);
        Assert.Equal(new[] { "Draw three lines." }, t.Goal);
        Assert.Equal(new[] { "My slopes are even.", "My curve is smooth." }, t.SelfChecks);
        Assert.Same(t, l.TryIt);
        Assert.Equal(new[] { "jaggies-bad", "jaggies-good" }, l.ExampleIds);
        Assert.Equal(9, b.Count);
    }

    [Fact]
    public void Parse_IsTolerant()
    {
        var text = "﻿#Title\r\n\r\nsummary : spaced\r\n<!-- a comment\r\n## not a heading -->\r\n[[ example : potion-16 ]]\r\n"
            + "[[mystery:x]]\r\n[[example:]]\r\nNote: this is text\r\ngroup: styles\r\n\tTabbed line\r\n";
        var l = LessonParser.Parse("x", text);
        Assert.Equal("Title", l.Title);
        Assert.Equal("spaced", l.Summary);
        Assert.Equal(LessonGroup.Fundamentals, l.Group); // "group:" after the content started is text
        Assert.Equal(0, l.Number);
        Assert.Equal("potion-16", Assert.IsType<ExampleBlock>(l.Blocks[0]).Examples[0].Id);
        Assert.Equal(new ParagraphBlock("Note: this is text group: styles Tabbed line"), l.Blocks[1]);
        Assert.Equal(2, l.Warnings.Count);
        Assert.DoesNotContain(l.Blocks, x => x is HeadingBlock);
    }

    [Fact]
    public void Parse_EmptyOrNull_UsesTheId()
    {
        var l = LessonParser.Parse("07-anti-aliasing", null);
        Assert.Equal("07-anti-aliasing", l.Title);
        Assert.Equal(7, l.Number);
        Assert.Empty(l.Blocks);
        Assert.Null(l.TryIt);
        Assert.Equal("07-anti-aliasing", LessonParser.Parse("07-anti-aliasing", "   \n\n").Title);
    }

    [Fact]
    public void Parse_UnknownGroup_Warns()
    {
        var l = LessonParser.Parse("a", "# A\ngroup: weird\n");
        Assert.Equal(LessonGroup.Fundamentals, l.Group);
        Assert.Single(l.Warnings);
    }

    [Fact]
    public void Parse_TryIt_EndsAtTheNextHeading()
    {
        var l = LessonParser.Parse("a", "# A\n## Try\n[[exercise:e]] Goal on the marker line\n- plain bullet\n## After\nText\n");
        var t = Assert.IsType<TryItBlock>(l.Blocks[1]);
        Assert.Equal(new[] { "Goal on the marker line", "plain bullet" }, t.Goal);
        Assert.Empty(t.SelfChecks);
        Assert.Equal(new ParagraphBlock("Text"), l.Blocks[^1]);
    }

    [Fact]
    public void Parse_ExamplesSeparatedByText_AreSeparateBlocks()
    {
        var l = LessonParser.Parse("a", "# A\n[[example:a]]\n\n[[example:b]]\ntext\n[[example:c]]\n");
        Assert.Equal(2, Assert.IsType<ExampleBlock>(l.Blocks[0]).Examples.Count);
        Assert.IsType<ParagraphBlock>(l.Blocks[1]);
        Assert.Single(Assert.IsType<ExampleBlock>(l.Blocks[2]).Examples);
    }

    [Fact]
    public void Parse_NumberedList_IsOrdered()
    {
        var l = LessonParser.Parse("a", "# A\n1. one\n2. two\n\n- x\n");
        Assert.True(Assert.IsType<ListBlock>(l.Blocks[0]).Ordered);
        Assert.Equal(new[] { "one", "two" }, ((ListBlock)l.Blocks[0]).Items);
        Assert.False(Assert.IsType<ListBlock>(l.Blocks[1]).Ordered);
    }

    [Fact]
    public void Inline_SplitsBold()
    {
        Assert.Equal(new[] { ("a ", false), ("b", true), (" c", false) }, Inline.Split("a **b** c"));
        Assert.Equal(new[] { ("a **b", false) }, Inline.Split("a **b"));
        Assert.Equal("a b c", Inline.Plain("a **b** c"));
        Assert.Empty(Inline.Split(""));
    }
}
