using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Draws the blocks <see cref="HtmlFormatter"/> produced: one selectable text block per paragraph with bold, italic and underlined runs.
/// Links look like links but do nothing (the address is in the tooltip), and nothing is fetched from the network.
/// </summary>
internal sealed class FormattedBodyPanel : StackPanel
{
    public static readonly StyledProperty<IReadOnlyList<HtmlBlock>?> BlocksProperty =
        AvaloniaProperty.Register<FormattedBodyPanel, IReadOnlyList<HtmlBlock>?>(nameof(Blocks));

    private const double IndentWidth = 20;

    static FormattedBodyPanel() => BlocksProperty.Changed.AddClassHandler<FormattedBodyPanel>((panel, _) => panel.Rebuild());

    public FormattedBodyPanel() => Spacing = 8;

    public IReadOnlyList<HtmlBlock>? Blocks
    {
        get => GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    private void Rebuild()
    {
        Children.Clear();
        foreach (var block in Blocks ?? [])
        {
            Children.Add(Build(block));
        }
    }

    private static Control Build(HtmlBlock block)
    {
        var text = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = block.IsPreformatted ? new FontFamily("Consolas, Cascadia Mono, monospace") : FontFamily.Default,
            FontSize = block.HeadingLevel switch
            {
                1 => 24,
                2 => 20,
                3 => 17,
                > 3 => 15,
                _ => 14,
            },
        };

        foreach (var run in block.Runs)
        {
            text.Inlines!.Add(new Run(run.Text)
            {
                FontWeight = run.Bold ? FontWeight.SemiBold : FontWeight.Normal,
                FontStyle = run.Italic ? FontStyle.Italic : FontStyle.Normal,
                TextDecorations = run.Underline ? TextDecorations.Underline : null,
            });
        }

        // One tooltip with the link addresses, since a block can hold several links and runs cannot carry tooltips.
        var links = block.Runs.Where(r => r.Href is not null).Select(r => r.Href!).Distinct(StringComparer.Ordinal).ToList();
        if (links.Count > 0)
        {
            ToolTip.SetTip(text, string.Join('\n', links));
        }

        Control result = text;
        if (block.IsQuote)
        {
            var quote = new Border
            {
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(10, 0, 0, 0),
                Child = text,
            };
            quote.Bind(Border.BorderBrushProperty, quote.GetResourceObservable("BorderSubtleBrush"));
            result = quote;
        }

        result.Margin = new Thickness(block.Indent * IndentWidth, 0, 0, 0);
        result.HorizontalAlignment = HorizontalAlignment.Stretch;
        return result;
    }
}
