using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal sealed record SalesOrderReasonDto(Guid Id, Guid SalesOrderId, string SalesOrderName, Guid SalesReasonId, string SalesReasonName)
{
    public static Expression<Func<SalesOrderReason, SalesOrderReasonDto>> Projection { get; } =
        t => new SalesOrderReasonDto(t.Id, t.SalesOrderId, t.SalesOrder.Number, t.SalesReasonId, t.SalesReason.Name);
}