using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal sealed record SalesTaxRateDto(Guid Id, string Name, string StateProvince, string TaxType, decimal TaxRate)
{
    public static Expression<Func<SalesTaxRate, SalesTaxRateDto>> Projection { get; } =
        t => new SalesTaxRateDto(t.Id, t.Name, t.StateProvince, t.TaxType, t.TaxRate);
}