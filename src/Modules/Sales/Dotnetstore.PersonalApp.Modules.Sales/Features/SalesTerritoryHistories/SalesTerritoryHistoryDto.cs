using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

internal sealed record SalesTerritoryHistoryDto(Guid Id, Guid SalesPersonId, string SalesPersonName, Guid TerritoryId, string TerritoryName, DateOnly StartDate, DateOnly? EndDate)
{
    public static Expression<Func<SalesTerritoryHistory, SalesTerritoryHistoryDto>> Projection { get; } =
        t => new SalesTerritoryHistoryDto(t.Id, t.SalesPersonId, t.SalesPerson.Name, t.TerritoryId, t.Territory.Name, t.StartDate, t.EndDate);
}