using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>An offer as the list shows it.</summary>
internal sealed record OfferDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerName,
    DateOnly Date,
    DateOnly ValidUntil,
    OfferStatus Status,
    bool IsExpired,
    decimal Total);

internal sealed record OfferLineDto(
    Guid Id,
    int Position,
    Guid? ProductId,
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

internal sealed record OfferDetailDto(
    OfferDto Offer,
    string? CustomerAddress,
    string? Reference,
    string? Notes,
    IReadOnlyList<OfferLineDto> Lines,
    OfferTotal Totals)
{
    public static OfferDetailDto From(Offer offer, DateOnly today)
    {
        var lines = offer.Lines
            .OrderBy(l => l.Position)
            .Select(l => new OfferLineDto(l.Id, l.Position, l.ProductId, l.Description, l.Quantity, l.Unit, l.UnitPrice, l.VatRate, l.DiscountPercent))
            .ToList();
        var totals = OfferTotals.Compute(lines.Select(l => l.Amounts));

        return new OfferDetailDto(ToDto(offer, totals.Gross, today), offer.CustomerAddress, offer.Reference, offer.Notes, lines, totals);
    }

    public static OfferDto ToDto(Offer offer, decimal total, DateOnly today) => new(
        offer.Id,
        offer.Number,
        offer.CustomerId,
        offer.CustomerName,
        offer.Date,
        offer.ValidUntil,
        offer.Status,
        offer.Status == OfferStatus.Sent && offer.ValidUntil < today,
        total);
}
