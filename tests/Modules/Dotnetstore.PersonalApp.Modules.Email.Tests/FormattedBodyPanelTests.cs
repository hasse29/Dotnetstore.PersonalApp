using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class FormattedBodyPanelTests
{
    private static HtmlRun Plain(string text) => new(text, false, false, false, null);

    private static HtmlBlock Block(IReadOnlyList<HtmlRun> runs, int indent = 0, int heading = 0, bool pre = false, bool quote = false) =>
        new(runs, indent, heading, pre, quote);

    private static SelectableTextBlock TextOf(Control child) => child switch
    {
        SelectableTextBlock text => text,
        Border { Child: SelectableTextBlock text } => text,
        _ => throw new InvalidOperationException("Unexpected child."),
    };

    [Fact]
    public Task NoBlocksMeansNoChildren() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel();

        panel.Children.ShouldBeEmpty();

        panel.Blocks = [Block([Plain("Hello")])];
        panel.Children.Count.ShouldBe(1);

        panel.Blocks = null;
        panel.Children.ShouldBeEmpty();
    });

    [Fact]
    public Task ABlockBecomesOneTextBlockWithEachRunStyled() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel
        {
            Blocks = [Block([new("Bold", true, false, false, null), new("Italic", false, true, false, null), new("Under", false, false, true, null), Plain("Plain")])],
        };

        var runs = TextOf(panel.Children.Single()).Inlines!.OfType<Run>().ToList();

        runs.Select(r => r.Text).ShouldBe(["Bold", "Italic", "Under", "Plain"]);
        runs[0].FontWeight.ShouldBe(FontWeight.SemiBold);
        runs[1].FontStyle.ShouldBe(FontStyle.Italic);
        runs[2].TextDecorations.ShouldBe(TextDecorations.Underline);
        runs[3].FontWeight.ShouldBe(FontWeight.Normal);
        runs[3].FontStyle.ShouldBe(FontStyle.Normal);
        runs[3].TextDecorations.ShouldBeNull();
    });

    [Theory]
    [InlineData(0, 14)]
    [InlineData(1, 24)]
    [InlineData(2, 20)]
    [InlineData(3, 17)]
    [InlineData(4, 15)]
    [InlineData(6, 15)]
    public Task HeadingLevelsGetTheirFontSize(int level, double size) => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel { Blocks = [Block([Plain("Title")], heading: level)] };

        TextOf(panel.Children.Single()).FontSize.ShouldBe(size);
    });

    [Fact]
    public Task PreformattedBlocksUseAMonospaceFont() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel { Blocks = [Block([Plain("code")], pre: true), Block([Plain("text")])] };

        TextOf(panel.Children[0]).FontFamily.ToString().ShouldContain("Consolas");
        TextOf(panel.Children[1]).FontFamily.ShouldBe(FontFamily.Default);
    });

    [Fact]
    public Task LinkAddressesGoInOneDistinctTooltip() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel
        {
            Blocks =
            [
                Block([new("a", false, false, false, "https://a.example"), new("b", false, false, false, "https://b.example"), new("a again", false, false, false, "https://a.example")]),
                Block([Plain("no links")]),
            ],
        };

        ToolTip.GetTip(TextOf(panel.Children[0])).ShouldBe("https://a.example\nhttps://b.example");
        ToolTip.GetTip(TextOf(panel.Children[1])).ShouldBeNull();
    });

    [Fact]
    public Task QuotesAreWrappedInABorderAndIndentIsAMarginInTwentyPixelSteps() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel
        {
            Blocks = [Block([Plain("quoted")], indent: 2, quote: true), Block([Plain("plain")], indent: 1)],
        };

        var quote = panel.Children[0].ShouldBeOfType<Border>();
        quote.BorderThickness.Left.ShouldBe(3);
        quote.Margin.Left.ShouldBe(40);
        panel.Children[1].ShouldBeOfType<SelectableTextBlock>().Margin.Left.ShouldBe(20);
    });

    [Fact]
    public Task SettingNewBlocksReplacesTheOldOnes() => Headless.Run(() =>
    {
        var panel = new FormattedBodyPanel { Blocks = [Block([Plain("one")]), Block([Plain("two")])] };

        panel.Blocks = [Block([Plain("three")])];

        panel.Children.Count.ShouldBe(1);
        TextOf(panel.Children[0]).Inlines!.OfType<Run>().Single().Text.ShouldBe("three");
    });
}
