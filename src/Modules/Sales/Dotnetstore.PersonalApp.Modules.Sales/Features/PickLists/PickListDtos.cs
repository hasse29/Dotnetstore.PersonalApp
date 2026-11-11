using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

internal sealed record PickListDto(
    Guid Id,
    string Number,
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    PickListStatus Status,
    int LineCount,
    int PickedLineCount,
    bool IsLocked);

internal sealed record PickListLineDto(
    Guid Id,
    int Position,
    Guid? ProductId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal PickedQuantity);

internal sealed record PickListDetailDto(PickListDto PickList, IReadOnlyList<PickListLineDto> Lines)
{
    public static PickListDetailDto From(PickList pickList, Order order)
    {
        var lines = pickList.Lines
            .OrderBy(l => l.Position)
            .Select(l => new PickListLineDto(l.Id, l.Position, l.ProductId, l.Description, l.Unit, l.Quantity, l.PickedQuantity))
            .ToList();

        return new PickListDetailDto(ToDto(pickList, order), lines);
    }

    public static PickListDto ToDto(PickList pickList, Order order) => new(
        pickList.Id,
        pickList.Number,
        order.Id,
        order.Number,
        order.CustomerName,
        pickList.Status,
        pickList.Lines.Count,
        pickList.Lines.Count(l => l.PickedQuantity >= l.Quantity),
        order.Status == OrderStatus.Invoiced);
}
