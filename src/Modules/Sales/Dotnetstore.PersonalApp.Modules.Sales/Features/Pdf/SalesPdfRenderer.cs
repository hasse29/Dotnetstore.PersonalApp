using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;

/// <summary>A column of a table in a sales document. <paramref name="Width"/> is relative to the other columns.</summary>
internal sealed record PdfColumn(string Header, float Width, bool RightAligned = false);

internal sealed record PdfTable(string? Title, IReadOnlyList<PdfColumn> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>Everything a sales document shows, already formatted and translated, so the renderer knows nothing about offers, orders or invoices.</summary>
internal sealed record PdfDocumentModel(
    string Title,
    string Number,
    string? Status,
    string CustomerLabel,
    string CustomerText,
    IReadOnlyList<(string Label, string Value)> Info,
    PdfTable Lines,
    IReadOnlyList<(string Label, string Value, bool Strong)> Totals,
    IReadOnlyList<PdfTable> Extra,
    string? NotesLabel,
    string? Notes,
    string PageLabel);

internal static class SalesPdfRenderer
{
    private const string Ink = "#1F2933";
    private const string Muted = "#6B7280";
    private const string Rule = "#D1D5DB";

    static SalesPdfRenderer()
    {
        // QuestPDF Community license: free below its revenue threshold, see https://www.questpdf.com/license/.
        QuestPDF.Settings.License = LicenseType.Community;

        // The bundled Lato has no CJK glyphs, but customers and lines may be written in Chinese. This is a desktop app that renders
        // on the user's own machine, so the installed system fonts are the right fallback (shipping a CJK font would add many MB).
        // A glyph no font has must never stop an invoice from being exported: it is drawn as a replacement character instead.
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public static byte[] Render(PdfDocumentModel model) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);

            page.DefaultTextStyle(style => style.FontSize(10).FontColor(Ink));

            page.Header().Element(container => Header(container, model));
            page.Content().PaddingTop(16).Element(container => Body(container, model));
            page.Footer().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                text.Span(model.PageLabel + " ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();

    private static void Header(IContainer container, PdfDocumentModel model) =>
        container.BorderBottom(1).BorderColor(Rule).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(model.Title).FontSize(22).Bold();
                column.Item().Text(model.Number).FontSize(13).FontColor(Muted);
            });

            if (model.Status is { Length: > 0 } status)
            {
                row.AutoItem().AlignBottom().Text(status).FontColor(Muted);
            }
        });

    private static void Body(IContainer container, PdfDocumentModel model) =>
        container.Column(column =>
        {
            column.Spacing(16);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(customer =>
                {
                    customer.Item().Text(model.CustomerLabel).FontSize(8).FontColor(Muted);
                    customer.Item().Text(model.CustomerText).Bold();
                });

                row.RelativeItem().Column(info =>
                {
                    foreach (var (label, value) in model.Info)
                    {
                        info.Item().Row(line =>
                        {
                            line.RelativeItem().Text(label).FontColor(Muted);
                            line.RelativeItem().AlignRight().Text(value);
                        });
                    }
                });
            });

            column.Item().Element(table => Table(table, model.Lines));

            if (model.Totals.Count > 0)
            {
                column.Item().AlignRight().Width(220).Column(totals =>
                {
                    foreach (var (label, value, strong) in model.Totals)
                    {
                        totals.Item().Row(line =>
                        {
                            var left = line.RelativeItem().Text(label);
                            var right = line.RelativeItem().AlignRight().Text(value);
                            if (strong)
                            {
                                left.Bold();
                                right.Bold();
                            }
                        });
                    }
                });
            }

            foreach (var extra in model.Extra)
            {
                column.Item().Element(table => Table(table, extra));
            }

            if (model.Notes is { Length: > 0 } notes)
            {
                column.Item().Column(note =>
                {
                    note.Item().Text(model.NotesLabel).FontSize(8).FontColor(Muted);
                    note.Item().Text(notes);
                });
            }
        });

    private static void Table(IContainer container, PdfTable model) =>
        container.Column(column =>
        {
            if (model.Title is { Length: > 0 } title)
            {
                column.Item().PaddingBottom(4).Text(title).Bold();
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var definition in model.Columns)
                    {
                        columns.RelativeColumn(definition.Width);
                    }
                });

                table.Header(header =>
                {
                    foreach (var definition in model.Columns)
                    {
                        var cell = header.Cell().BorderBottom(1).BorderColor(Ink).PaddingVertical(4);
                        (definition.RightAligned ? cell.AlignRight() : cell).Text(definition.Header).FontSize(8).Bold();
                    }
                });

                foreach (var row in model.Rows)
                {
                    for (var i = 0; i < model.Columns.Count; i++)
                    {
                        var cell = table.Cell().BorderBottom(0.5f).BorderColor(Rule).PaddingVertical(4);
                        (model.Columns[i].RightAligned ? cell.AlignRight() : cell).Text(i < row.Count ? row[i] : string.Empty);
                    }
                }
            });
        });
}
