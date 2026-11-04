using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;

internal sealed record CurrencyRateDto(Guid Id, DateOnly RateDate, Guid FromCurrencyId, string FromCurrencyName, Guid ToCurrencyId, string ToCurrencyName, decimal AverageRate, decimal EndOfDayRate)
{
    public static Expression<Func<CurrencyRate, CurrencyRateDto>> Projection { get; } =
        t => new CurrencyRateDto(t.Id, t.RateDate, t.FromCurrencyId, t.FromCurrency.Name, t.ToCurrencyId, t.ToCurrency.Name, t.AverageRate, t.EndOfDayRate);
}