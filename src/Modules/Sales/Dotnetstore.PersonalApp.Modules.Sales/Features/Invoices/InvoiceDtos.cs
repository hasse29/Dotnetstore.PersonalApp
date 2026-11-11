using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>An invoice as the list shows it. The invoice must have its lines and payments loaded.</summary>
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
    decimal Paid)
{
    public decimal Outstanding => Status == InvoiceStatus.Cancelled ? 0m : Math.Max(Total - Paid, 0m);
}

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

internal sealed record PaymentDto(Guid Id, PaymentKind Kind, DateOnly Date, decimal Amount, decimal Excess, string? Note);

internal sealed record InvoiceDetailDto(
    InvoiceDto Invoice,
    string? CustomerAddress,
    string? Reference,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines,
    OfferTotal Totals,
    IReadOnlyList<PaymentDto> Payments,
    decimal CustomerCredit)
{
    /// <param name="invoice">An invoice with its lines and payments loaded.</param>
    /// <param name="creditBalance">What the customer has in credit, which can be applied to this invoice.</param>
    public static InvoiceDetailDto From(Invoice invoice, DateOnly today, decimal creditBalance = 0m)
    {
        var lines = invoice.Lines
            .OrderBy(l => l.Position)
            .Select(l => new InvoiceLineDto(l.Id, l.Position, l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent))
            .ToList();
        var totals = OfferTotals.Compute(lines.Select(l => l.Amounts));
        var payments = invoice.Payments
            .OrderBy(p => p.Date)
            .ThenBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new PaymentDto(p.Id, p.Kind, p.Date, p.Amount, p.Excess, p.Note))
            .ToList();

        return new InvoiceDetailDto(ToDto(invoice, totals.Gross, today), invoice.CustomerAddress, invoice.Reference, invoice.Notes, lines, totals, payments, creditBalance);
    }

    public static InvoiceDto ToDto(Invoice invoice, decimal total, DateOnly today) => new(
        invoice.Id,
        invoice.Number,
        invoice.OrderNumber,
        invoice.CustomerName,
        invoice.Date,
        invoice.DueDate,
        invoice.Status,
        invoice.Status is InvoiceStatus.Unpaid or InvoiceStatus.PartiallyPaid && invoice.DueDate < today,
        total,
        InvoicePayments.Paid(invoice));
}
