using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal sealed record SalesPersonQuotaHistoryDto(Guid Id, Guid SalesPersonId, string SalesPersonName, DateOnly QuotaDate, decimal SalesQuota)
{
    public static Expression<Func<SalesPersonQuotaHistory, SalesPersonQuotaHistoryDto>> Projection { get; } =
        t => new SalesPersonQuotaHistoryDto(t.Id, t.SalesPersonId, t.SalesPerson.Name, t.QuotaDate, t.SalesQuota);
}