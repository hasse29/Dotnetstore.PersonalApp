using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>Goods left the stock: sold, used or thrown away. Refused when there is not enough at the location.</summary>
internal static class IssueStock
{
    public sealed record Command(Guid ProductId, Guid LocationId, decimal Quantity, string? Note);

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

            if (await StockLedger.CheckLocationAsync(db, command.LocationId, requireActive: false, cancellationToken).ConfigureAwait(false) is { } locationError)
            {
                return locationError;
            }

            var applied = await StockLedger.ApplyAsync(
                db, command.ProductId, command.LocationId, -command.Quantity, StockMovementType.Issue, clock.GetUtcNow(),
                StockLedger.NormalizeNote(command.Note), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied.IsSuccess)
            {
                return applied;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return applied;
        }
    }
}
