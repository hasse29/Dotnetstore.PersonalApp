using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal sealed record SalesPersonDto(Guid Id, string Name, Guid? UserId, string? UserName, Guid? TerritoryId, string? TerritoryName, decimal? SalesQuota, decimal Bonus, decimal CommissionPct, decimal SalesYtd, decimal SalesLastYear)
{
    public static Expression<Func<SalesPerson, SalesPersonDto>> Projection { get; } =
        t => new SalesPersonDto(t.Id, t.Name, t.UserId, null, t.TerritoryId, t.Territory == null ? null : t.Territory.Name, t.SalesQuota, t.Bonus, t.CommissionPct, t.SalesYtd, t.SalesLastYear);

    public SalesPersonDto WithNames(IReadOnlyDictionary<Guid, string> userNames) => this with { UserName = ExternalReferences.NameOf(userNames, UserId) };
}