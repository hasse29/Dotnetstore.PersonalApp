using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>
/// The one place stock changes. Every change is a <see cref="StockMovement"/> plus the matching <see cref="StockLevel"/> update;
/// callers add as many as they need to the context and save once, so a transfer or a posted count is all or nothing.
/// </summary>
internal static class StockLedger
{
    public const decimal MaxQuantity = 1_000_000m;
    public const int MaxNoteLength = 500;

    public static IRuleBuilderOptions<T, decimal> QuantityRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(q => q > 0m && q <= MaxQuantity)
            .WithErrorCode("stock_quantity_invalid").WithMessage("Quantity must be more than 0 and at most 1 000 000.");

    public static IRuleBuilderOptions<T, string?> NoteRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNoteLength).WithErrorCode("stock_note_too_long").WithMessage("Note is too long.");

    public static string? NormalizeNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    /// <summary>Consulting hours are not goods, so they cannot be in stock.</summary>
    public static async Task<Error?> CheckProductAsync(ProductsDbContext db, Guid productId, CancellationToken cancellationToken)
    {
        var kind = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => (ProductKind?)p.Kind)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (kind is null)
        {
            return Error.NotFound("product_not_found", "Product not found.");
        }

        return kind == ProductKind.Consulting ? Error.Validation("product_not_stockable", "Consulting cannot be kept in stock.") : null;
    }

    /// <param name="requireActive">True when stock is added to the location; stock can always be taken out of an inactive one.</param>
    public static async Task<Error?> CheckLocationAsync(ProductsDbContext db, Guid locationId, bool requireActive, CancellationToken cancellationToken)
    {
        var location = await db.StorageLocations.AsNoTracking()
            .Where(l => l.Id == locationId)
            .Select(l => new { l.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (location is null)
        {
            return Error.NotFound("storage_location_not_found", "Storage location not found.");
        }

        return requireActive && !location.IsActive
            ? Error.Validation("storage_location_inactive", "The storage location is inactive.")
            : null;
    }

    /// <summary>Adds the movement and moves the level by <paramref name="delta"/>; fails, adding nothing, if that would make the level negative.</summary>
    /// <returns>The new quantity at the location.</returns>
    public static async Task<Result<decimal>> ApplyAsync(
        ProductsDbContext db,
        Guid productId,
        Guid locationId,
        decimal delta,
        StockMovementType type,
        DateTimeOffset at,
        string? note = null,
        Guid? supplierId = null,
        Guid? transferId = null,
        CancellationToken cancellationToken = default)
    {
        // A transfer or a count can touch the same level twice before saving, so unsaved levels must be found too.
        var level = db.StockLevels.Local.FirstOrDefault(l => l.ProductId == productId && l.LocationId == locationId)
            ?? await db.StockLevels.FirstOrDefaultAsync(l => l.ProductId == productId && l.LocationId == locationId, cancellationToken).ConfigureAwait(false);

        var quantity = (level?.Quantity ?? 0m) + delta;
        if (quantity < 0m)
        {
            return Error.Conflict("stock_insufficient", "There is not enough in stock.");
        }

        if (level is null)
        {
            level = new StockLevel { Id = Guid.CreateVersion7(), ProductId = productId, LocationId = locationId };
            db.StockLevels.Add(level);
        }

        level.Quantity = quantity;

        db.StockMovements.Add(new StockMovement
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            LocationId = locationId,
            Type = type,
            Quantity = delta,
            SupplierId = supplierId,
            Note = note,
            TransferId = transferId,
            OccurredAt = at,
        });

        return quantity;
    }
}
