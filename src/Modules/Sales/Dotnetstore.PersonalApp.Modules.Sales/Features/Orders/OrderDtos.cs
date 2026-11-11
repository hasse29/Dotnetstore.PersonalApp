using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

/// <summary>An order as the list shows it. <paramref name="PickedLines"/> and <paramref name="PickLines"/> are zero when nothing has to be picked.</summary>
internal sealed record OrderDto(
    Guid Id,
    string Number,
    string OfferNumber,
    string CustomerName,
    DateOnly Date,
    OrderStatus Status,
    decimal Total,
    int PickedLines,
    int PickLines);

internal sealed record OrderLineDto(
    Guid Id,
    int Position,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountPercent,
    bool IsPhysical)
{
    public LineAmounts Amounts => new(Quantity, UnitPrice, VatRate, DiscountPercent);

    public decimal Net => OfferTotals.LineNet(Amounts);
}

internal sealed record OrderDetailDto(
    OrderDto Order,
    Guid OfferId,
    string? CustomerAddress,
    string? Reference,
    string? Notes,
    IReadOnlyList<OrderLineDto> Lines,
    OfferTotal Totals,
    string? PickListNumber,
    string? InvoiceNumber = null)
{
    /// <param name="order">An order with its lines and pick list (with its lines) loaded.</param>
    public static OrderDetailDto From(Order order)
    {
        var lines = order.Lines
            .OrderBy(l => l.Position)
            .Select(l => new OrderLineDto(l.Id, l.Position, l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent, l.IsPhysical))
            .ToList();
        var totals = OfferTotals.Compute(lines.Select(l => l.Amounts));

        return new OrderDetailDto(
            ToDto(order, totals.Gross),
            order.OfferId,
            order.CustomerAddress,
            order.Reference,
            order.Notes,
            lines,
            totals,
            order.PickList?.Number);
    }

    public static OrderDto ToDto(Order order, decimal total) => new(
        order.Id,
        order.Number,
        order.OfferNumber,
        order.CustomerName,
        order.Date,
        order.Status,
        total,
        order.PickList?.Lines.Count(l => l.PickedQuantity >= l.Quantity) ?? 0,
        order.PickList?.Lines.Count ?? 0);
}
