using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>An invoice as the list shows it.</summary>
internal sealed record InvoiceDto(
    Guid Id,
    string Number,
    string OrderNumber,
    string CustomerName,
    DateOnly Date,
    DateOnly DueDate,
    InvoiceStatus Status,
    bool IsOverdue,
    decimal Total,
    DateOnly? PaidDate);

internal sealed record InvoiceLineDto(
    Guid Id,
    int Position,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountPercent)
{
    public LineAmounts Amounts => new(Quantity, UnitPrice, VatRate, DiscountPercent);

    public decimal Net => OfferTotals.LineNet(Amounts);
}

internal sealed record InvoiceDetailDto(
    InvoiceDto Invoice,
    string? CustomerAddress,
    string? Reference,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines,
    OfferTotal Totals)
{
    /// <param name="invoice">An invoice with its lines loaded.</param>
    public static InvoiceDetailDto From(Invoice invoice, DateOnly today)
    {
        var lines = invoice.Lines
            .OrderBy(l => l.Position)
            .Select(l => new InvoiceLineDto(l.Id, l.Position, l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent))
            .ToList();
        var totals = OfferTotals.Compute(lines.Select(l => l.Amounts));

        return new InvoiceDetailDto(ToDto(invoice, totals.Gross, today), invoice.CustomerAddress, invoice.Reference, invoice.Notes, lines, totals);
    }

    public static InvoiceDto ToDto(Invoice invoice, decimal total, DateOnly today) => new(
        invoice.Id,
        invoice.Number,
        invoice.OrderNumber,
        invoice.CustomerName,
        invoice.Date,
        invoice.DueDate,
        invoice.Status,
        invoice.Status == InvoiceStatus.Unpaid && invoice.DueDate < today,
        total,
        invoice.PaidDate);
}
