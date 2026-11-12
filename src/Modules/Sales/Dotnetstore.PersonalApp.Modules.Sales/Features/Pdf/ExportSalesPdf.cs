using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;

internal enum SalesDocumentKind
{
    Offer,
    Order,
    PickList,
    Invoice,
}

/// <summary>
/// Makes a PDF of an offer, order, pick list or invoice, in the current UI language. The document is read through the
/// same handlers the screens use, so the PDF shows exactly what the screen shows.
/// </summary>
internal static class ExportSalesPdf
{
    public sealed record Query(SalesDocumentKind Kind, Guid Id);

    public sealed record Response(string FileName, byte[] Content);

    public sealed class Handler(
        GetOffer.Handler offers,
        GetOrder.Handler orders,
        GetPickList.Handler pickLists,
        GetInvoice.Handler invoices,
        ILocalizationService localization)
    {
        private delegate Task<Result<PdfDocumentModel>> Builder(Guid id, CancellationToken cancellationToken);

        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            Builder? build = query.Kind switch
            {
                SalesDocumentKind.Offer => OfferAsync,
                SalesDocumentKind.Order => OrderAsync,
                SalesDocumentKind.PickList => PickListAsync,
                SalesDocumentKind.Invoice => InvoiceAsync,
                _ => null,
            };
            if (build is null)
            {
                return Error.Validation("sales_pdf_kind_invalid", "That document cannot be exported.");
            }

            var model = await build(query.Id, cancellationToken).ConfigureAwait(false);
            if (!model.IsSuccess)
            {
                return model.Error;
            }

            return new Response(FileName(model.Value), SalesPdfRenderer.Render(model.Value));
        }

        private static string FileName(PdfDocumentModel model)
        {
            var name = $"{model.Title} {model.Number}";
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '-');
            }

            return name + ".pdf";
        }

        private async Task<Result<PdfDocumentModel>> OfferAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = await offers.HandleAsync(new GetOffer.Query(id), cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var detail = result.Value;
            var offer = detail.Offer;
            List<(string, string)> info = [(T("SalesPdf_Date"), Date(offer.Date)), (T("SalesPdf_ValidUntil"), Date(offer.ValidUntil))];
            AddReference(info, detail.Reference);

            return Model(
                "SalesPdf_Offer",
                offer.Number,
                T(offer.IsExpired ? "OfferStatus_Expired" : $"OfferStatus_{offer.Status}"),
                offer.CustomerName,
                detail.CustomerAddress,
                info,
                PricedLines(detail.Lines.Select(l => (l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent, l.Net))),
                Totals(detail.Totals),
                [],
                detail.Notes);
        }

        private async Task<Result<PdfDocumentModel>> OrderAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = await orders.HandleAsync(new GetOrder.Query(id), cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var detail = result.Value;
            var order = detail.Order;
            List<(string, string)> info = [(T("SalesPdf_Date"), Date(order.Date)), (T("SalesPdf_FromOffer"), order.OfferNumber)];
            AddReference(info, detail.Reference);

            return Model(
                "SalesPdf_Order",
                order.Number,
                T($"OrderStatus_{order.Status}"),
                order.CustomerName,
                detail.CustomerAddress,
                info,
                PricedLines(detail.Lines.Select(l => (l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent, l.Net))),
                Totals(detail.Totals),
                [],
                detail.Notes);
        }

        private async Task<Result<PdfDocumentModel>> PickListAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = await pickLists.HandleAsync(new GetPickList.Query(id), cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var pickList = result.Value.PickList;
            PdfColumn[] columns =
            [
                new("#", 0.5f),
                new(T("SalesPdf_ColDescription"), 5),
                new(T("SalesPdf_ColOrdered"), 1.5f, RightAligned: true),
                new(T("SalesPdf_ColPicked"), 1.5f, RightAligned: true),
            ];

            // An empty line to write on is what makes a printed pick list useful on the shelf.
            var rows = result.Value.Lines
                .Select(l => (IReadOnlyList<string>)
                [
                    l.Position.ToString(CultureInfo.CurrentCulture),
                    l.Description,
                    Quantity(l.Quantity, l.Unit),
                    l.PickedQuantity > 0m ? Quantity(l.PickedQuantity, l.Unit) : "______",
                ])
                .ToList();

            return Model(
                "SalesPdf_PickList",
                pickList.Number,
                T($"PickListStatus_{pickList.Status}"),
                pickList.CustomerName,
                null,
                [(T("SalesPdf_FromOrder"), pickList.OrderNumber)],
                new PdfTable(null, columns, rows),
                [],
                [],
                null);
        }

        private async Task<Result<PdfDocumentModel>> InvoiceAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = await invoices.HandleAsync(new GetInvoice.Query(id), cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var detail = result.Value;
            var invoice = detail.Invoice;
            List<(string, string)> info =
            [
                (T("SalesPdf_Date"), Date(invoice.Date)),
                (T("SalesPdf_DueDate"), Date(invoice.DueDate)),
                (T("SalesPdf_FromOrder"), invoice.OrderNumber),
            ];
            AddReference(info, detail.Reference);

            var totals = Totals(detail.Totals);
            if (invoice.Paid > 0m)
            {
                totals.Add((T("SalesPdf_Paid"), Money(invoice.Paid), false));
                totals.Add((T("SalesPdf_Outstanding"), Money(invoice.Outstanding), true));
            }

            List<PdfTable> extra = [];
            if (detail.Payments.Count > 0)
            {
                PdfColumn[] columns =
                [
                    new(T("SalesPdf_Date"), 2),
                    new(T("SalesPdf_ColPayment"), 5),
                    new(T("SalesPdf_ColAmount"), 2, RightAligned: true),
                ];
                var rows = detail.Payments
                    .Select(p => (IReadOnlyList<string>)
                    [
                        Date(p.Date),
                        T($"PaymentKind_{p.Kind}") + (string.IsNullOrEmpty(p.Note) ? string.Empty : $" - {p.Note}"),
                        Money(p.Amount + p.Excess),
                    ])
                    .ToList();
                extra.Add(new PdfTable(T("SalesPdf_Payments"), columns, rows));
            }

            return Model(
                "SalesPdf_Invoice",
                invoice.Number,
                T(invoice.IsOverdue ? "InvoiceStatus_Overdue" : $"InvoiceStatus_{invoice.Status}"),
                invoice.CustomerName,
                detail.CustomerAddress,
                info,
                PricedLines(detail.Lines.Select(l => (l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent, l.Net))),
                totals,
                extra,
                detail.Notes);
        }

        private PdfDocumentModel Model(
            string titleKey,
            string number,
            string status,
            string customerName,
            string? customerAddress,
            IReadOnlyList<(string Label, string Value)> info,
            PdfTable lines,
            IReadOnlyList<(string Label, string Value, bool Strong)> totals,
            IReadOnlyList<PdfTable> extra,
            string? notes) => new(
            T(titleKey),
            number,
            status,
            T("SalesPdf_Customer"),
            customerAddress is { Length: > 0 } ? $"{customerName}\n{customerAddress}" : customerName,
            info,
            lines,
            totals,
            extra,
            T("SalesPdf_Notes"),
            notes,
            T("SalesPdf_Page"));

        private PdfTable PricedLines(IEnumerable<(string Description, decimal Quantity, string Unit, decimal UnitPrice, decimal VatRate, decimal Discount, decimal Net)> lines)
        {
            PdfColumn[] columns =
            [
                new("#", 0.5f),
                new(T("SalesPdf_ColDescription"), 4),
                new(T("SalesPdf_ColQuantity"), 1.5f, RightAligned: true),
                new(T("SalesPdf_ColUnitPrice"), 1.5f, RightAligned: true),
                new(T("SalesPdf_ColDiscount"), 1.2f, RightAligned: true),
                new(T("SalesPdf_ColVat"), 1.1f, RightAligned: true),
                new(T("SalesPdf_ColAmount"), 1.7f, RightAligned: true),
            ];
            var rows = lines
                .Select((l, index) => (IReadOnlyList<string>)
                [
                    (index + 1).ToString(CultureInfo.CurrentCulture),
                    l.Description,
                    Quantity(l.Quantity, l.Unit),
                    Money(l.UnitPrice),
                    l.Discount > 0m ? string.Create(CultureInfo.CurrentCulture, $"{l.Discount:0.##} %") : string.Empty,
                    string.Create(CultureInfo.CurrentCulture, $"{l.VatRate:0.##} %"),
                    Money(l.Net),
                ])
                .ToList();

            return new PdfTable(null, columns, rows);
        }

        private List<(string Label, string Value, bool Strong)> Totals(OfferTotal totals) =>
        [
            (T("SalesPdf_Net"), Money(totals.Net), false),
            (T("SalesPdf_Vat"), Money(totals.Vat), false),
            (T("SalesPdf_Total"), Money(totals.Gross), true),
        ];

        private void AddReference(List<(string, string)> info, string? reference)
        {
            if (!string.IsNullOrWhiteSpace(reference))
            {
                info.Add((T("SalesPdf_Reference"), reference));
            }
        }

        private string Quantity(decimal quantity, string unit) =>
            string.Create(CultureInfo.CurrentCulture, $"{quantity:0.##} {localization.Find($"ProductUnit_{unit}") ?? unit}");

        private string T(string key) => localization.Get(key);

        private static string Date(DateOnly date) => date.ToString("d", CultureInfo.CurrentCulture);

        private static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);
    }
}

/// <summary>What a screen's Export PDF button does: make the PDF, let the user save it, and say how it went.</summary>
internal sealed class SalesPdfSaver(ExportSalesPdf.Handler export, IFileSaveService fileSaver, ILocalizationService localization)
{
    /// <returns>The status message to show, or null when the user cancelled the save dialog.</returns>
    public async Task<string?> SaveAsync(SalesDocumentKind kind, Guid id, CancellationToken cancellationToken)
    {
        var result = await export.HandleAsync(new ExportSalesPdf.Query(kind, id), cancellationToken);
        if (!result.IsSuccess)
        {
            return localization.Describe(result.Error);
        }

        var saved = await fileSaver.SaveAsync(localization.Get("SalesPdf_DialogTitle"), result.Value.FileName, result.Value.Content, cancellationToken);
        return saved is null ? null : localization.Format("SalesPdf_Saved", saved);
    }
}
