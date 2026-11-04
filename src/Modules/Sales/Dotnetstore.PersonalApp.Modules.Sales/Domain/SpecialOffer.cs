namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>special offer.</summary>
internal sealed class SpecialOffer
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal DiscountPct { get; set; }

    public string OfferType { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int MinQty { get; set; }

    public int? MaxQty { get; set; }
}