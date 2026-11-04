namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>offer product.</summary>
internal sealed class SpecialOfferProduct
{
    public Guid Id { get; set; }

    public Guid SpecialOfferId { get; set; }

    public SpecialOffer SpecialOffer { get; set; } = null!;

    public Guid ProductId { get; set; }
}