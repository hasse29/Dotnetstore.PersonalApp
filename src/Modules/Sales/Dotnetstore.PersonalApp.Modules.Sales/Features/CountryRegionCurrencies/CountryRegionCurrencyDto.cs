using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;

internal sealed record CountryRegionCurrencyDto(Guid Id, string CountryRegion, Guid CurrencyId, string CurrencyName)
{
    public static Expression<Func<CountryRegionCurrency, CountryRegionCurrencyDto>> Projection { get; } =
        t => new CountryRegionCurrencyDto(t.Id, t.CountryRegion, t.CurrencyId, t.Currency.Name);
}