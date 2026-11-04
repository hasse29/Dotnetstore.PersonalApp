using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

internal sealed record CurrencyDto(Guid Id, string Code, string Name)
{
    public static Expression<Func<Currency, CurrencyDto>> Projection { get; } =
        t => new CurrencyDto(t.Id, t.Code, t.Name);
}