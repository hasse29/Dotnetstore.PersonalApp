using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;

internal sealed record SpecialOfferProductDto(Guid Id, Guid SpecialOfferId, string SpecialOfferName, Guid ProductId, string ProductName)
{
    public static Expression<Func<SpecialOfferProduct, SpecialOfferProductDto>> Projection { get; } =
        t => new SpecialOfferProductDto(t.Id, t.SpecialOfferId, t.SpecialOffer.Description, t.ProductId, string.Empty);

    public SpecialOfferProductDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}