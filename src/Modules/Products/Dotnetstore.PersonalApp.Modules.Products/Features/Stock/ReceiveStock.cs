using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>Goods came in, optionally from a supplier.</summary>
internal static class ReceiveStock
{
    public sealed record Command(Guid ProductId, Guid LocationId, decimal Quantity, Guid? SupplierId, string? Note);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Quantity).QuantityRules();
            RuleFor(c => c.Note).NoteRules();
        }
    }

    /// <returns>The new quantity at the location.</returns>
    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<decimal>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<decimal>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                var failure = validation.Errors[0];
                return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await StockLedger.CheckProductAsync(db, command.ProductId, cancellationToken).ConfigureAwait(false) is { } productError)
            {
                return productError;
            }

            if (await StockLedger.CheckLocationAsync(db, command.LocationId, requireActive: true, cancellationToken).ConfigureAwait(false) is { } locationError)
            {
                return locationError;
            }

            if (command.SupplierId is { } supplierId
                && !await db.Suppliers.AnyAsync(s => s.Id == supplierId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("supplier_not_found", "Supplier not found.");
            }

            var applied = await StockLedger.ApplyAsync(
                db, command.ProductId, command.LocationId, command.Quantity, StockMovementType.Receipt, clock.GetUtcNow(),
                StockLedger.NormalizeNote(command.Note), command.SupplierId, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied.IsSuccess)
            {
                return applied;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return applied;
        }
    }
}
