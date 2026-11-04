using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

internal sealed record SpecialOfferDto(Guid Id, string Description, decimal DiscountPct, string OfferType, string Category, DateOnly StartDate, DateOnly EndDate, int MinQty, int? MaxQty)
{
    public static Expression<Func<SpecialOffer, SpecialOfferDto>> Projection { get; } =
        t => new SpecialOfferDto(t.Id, t.Description, t.DiscountPct, t.OfferType, t.Category, t.StartDate, t.EndDate, t.MinQty, t.MaxQty);
}