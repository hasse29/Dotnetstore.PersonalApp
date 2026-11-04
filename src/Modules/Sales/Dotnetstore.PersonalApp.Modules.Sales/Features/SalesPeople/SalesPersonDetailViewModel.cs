using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

/// <summary>A salesperson with the history of quotas and territories.</summary>
internal sealed class SalesPersonDetailViewModel(
    SalesPersonDto salesPerson,
    SalesPersonQuotaHistoriesViewModel quotaHistory,
    SalesTerritoryHistoriesViewModel territoryHistory) : DetailViewModel
{
    public SalesPersonDto SalesPerson { get; } = salesPerson;

    public SalesPersonQuotaHistoriesViewModel QuotaHistory { get; } = quotaHistory.ForParent(salesPerson.Id);

    public SalesTerritoryHistoriesViewModel TerritoryHistory { get; } = territoryHistory.ForParent(salesPerson.Id);
}
