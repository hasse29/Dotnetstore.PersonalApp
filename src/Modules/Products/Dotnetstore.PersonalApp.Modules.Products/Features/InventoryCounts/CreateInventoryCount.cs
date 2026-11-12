using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>
/// Starts a count and fills it with a line per product and location that has stock. With a location chosen, every other stockable
/// product can be listed there too (expected 0), so things found on the shelf that the system did not know about can be entered.
/// </summary>
internal static class CreateInventoryCount
{
    public const int MaxNameLength = 100;
    public const int MaxNoteLength = 500;

    public sealed record Command(string Name, Guid? LocationId, string? Note, bool IncludeUnstocked);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("inventory_count_name_required").WithMessage("Name is required.")
                .MaximumLength(MaxNameLength).WithErrorCode("inventory_count_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.Note).MaximumLength(MaxNoteLength).WithErrorCode("inventory_count_note_too_long").WithMessage("Note is too long.");
            RuleFor(c => c).Must(c => !c.IncludeUnstocked || c.LocationId is not null)
                .WithErrorCode("inventory_count_location_required").WithMessage("Choose a location to include products without stock.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<InventoryCountDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<InventoryCountDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                var failure = validation.Errors[0];
                return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            string? locationName = null;
            if (command.LocationId is { } locationId)
            {
                locationName = await db.StorageLocations.AsNoTracking()
                    .Where(l => l.Id == locationId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (locationName is null)
                {
                    return Error.NotFound("storage_location_not_found", "Storage location not found.");
                }
            }

            var levels = db.StockLevels.AsNoTracking().Where(l => l.Quantity > 0m);
            if (command.LocationId is { } filter)
            {
                levels = levels.Where(l => l.LocationId == filter);
            }

            var count = new InventoryCount
            {
                Id = Guid.CreateVersion7(),
                Name = command.Name.Trim(),
                LocationId = command.LocationId,
                Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
                CreatedAt = clock.GetUtcNow(),
            };

            foreach (var level in await levels.ToListAsync(cancellationToken).ConfigureAwait(false))
            {
                count.Lines.Add(new InventoryCountLine
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = level.ProductId,
                    LocationId = level.LocationId,
                    ExpectedQuantity = level.Quantity,
                });
            }

            if (command.IncludeUnstocked && command.LocationId is { } onlyLocation)
            {
                var present = count.Lines.Select(l => l.ProductId).ToHashSet();
                var others = await db.Products.AsNoTracking()
                    .Where(p => p.Kind != ProductKind.Consulting && p.IsActive)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var productId in others.Where(id => !present.Contains(id)))
                {
                    count.Lines.Add(new InventoryCountLine
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = productId,
                        LocationId = onlyLocation,
                        ExpectedQuantity = 0m,
                    });
                }
            }

            db.InventoryCounts.Add(count);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new InventoryCountDto(count.Id, count.Name, count.LocationId, locationName, count.Note, count.CreatedAt, null, count.Lines.Count, 0);
        }
    }
}
