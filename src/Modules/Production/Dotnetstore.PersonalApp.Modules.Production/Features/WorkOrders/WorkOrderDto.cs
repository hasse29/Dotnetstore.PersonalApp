using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal sealed record WorkOrderDto(Guid Id, string Number, Guid ProductId, string ProductName, int OrderQty, int ScrappedQty, DateOnly StartDate, DateOnly? EndDate, DateOnly DueDate, Guid? ScrapReasonId, string? ScrapReasonName)
{
    public static Expression<Func<WorkOrder, WorkOrderDto>> Projection { get; } =
        t => new WorkOrderDto(t.Id, t.Number, t.ProductId, t.Product.Name, t.OrderQty, t.ScrappedQty, t.StartDate, t.EndDate, t.DueDate, t.ScrapReasonId, t.ScrapReason == null ? null : t.ScrapReason.Name);
}