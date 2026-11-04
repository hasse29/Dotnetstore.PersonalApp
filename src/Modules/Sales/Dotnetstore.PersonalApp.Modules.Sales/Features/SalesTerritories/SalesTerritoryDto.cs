using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;

internal sealed record SalesTerritoryDto(Guid Id, string Name, string CountryRegionCode, string RegionGroup, decimal SalesYtd, decimal SalesLastYear, decimal CostYtd, decimal CostLastYear)
{
    public static Expression<Func<SalesTerritory, SalesTerritoryDto>> Projection { get; } =
        t => new SalesTerritoryDto(t.Id, t.Name, t.CountryRegionCode, t.RegionGroup, t.SalesYtd, t.SalesLastYear, t.CostYtd, t.CostLastYear);
}