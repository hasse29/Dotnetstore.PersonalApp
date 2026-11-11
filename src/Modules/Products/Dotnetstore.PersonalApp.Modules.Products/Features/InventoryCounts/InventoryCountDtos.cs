using Dotnetstore.PersonalApp.Modules.Products.Domain;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <param name="LocationName">Null when every location is counted.</param>
internal sealed record InventoryCountDto(
    Guid Id,
    string Name,
    Guid? LocationId,
    string? LocationName,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PostedAt,
    int LineCount,
    int CountedLineCount)
{
    public bool IsPosted => PostedAt is not null;
}

internal sealed record InventoryCountLineDto(
    Guid Id,
    Guid ProductId,
    string ProductNumber,
    string ProductName,
    ProductUnit Unit,
    Guid LocationId,
    string LocationName,
    decimal ExpectedQuantity,
    decimal? CountedQuantity);

internal sealed record InventoryCountDetailDto(InventoryCountDto Count, IReadOnlyList<InventoryCountLineDto> Lines);
