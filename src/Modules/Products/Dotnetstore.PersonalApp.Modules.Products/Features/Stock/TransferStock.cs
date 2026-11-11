using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>Moves goods from one location to another as a pair of movements that succeed or fail together.</summary>
internal static class TransferStock
{
    public sealed record Command(Guid ProductId, Guid FromLocationId, Guid ToLocationId, decimal Quantity, string? Note);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Quantity).QuantityRules();
            RuleFor(c => c.Note).NoteRules();
            RuleFor(c => c).Must(c => c.FromLocationId != c.ToLocationId)
                .WithErrorCode("stock_same_location").WithMessage("Choose two different locations.");
        }
    }

    /// <returns>The new quantity at the sending location.</returns>
    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public async Task<Result<decimal>> HandleAsync(Command command, CancellationToken cancellationToken)
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

            if (await StockLedger.CheckLocationAsync(db, command.FromLocationId, requireActive: false, cancellationToken).ConfigureAwait(false) is { } fromError)
            {
                return fromError;
            }

            if (await StockLedger.CheckLocationAsync(db, command.ToLocationId, requireActive: true, cancellationToken).ConfigureAwait(false) is { } toError)
            {
                return toError;
            }

            var now = clock.GetUtcNow();
            var note = StockLedger.NormalizeNote(command.Note);
            var transferId = Guid.CreateVersion7();

            var sent = await StockLedger.ApplyAsync(
                db, command.ProductId, command.FromLocationId, -command.Quantity, StockMovementType.TransferOut, now,
                note, transferId: transferId, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!sent.IsSuccess)
            {
                return sent;
            }

            await StockLedger.ApplyAsync(
                db, command.ProductId, command.ToLocationId, command.Quantity, StockMovementType.TransferIn, now,
                note, transferId: transferId, cancellationToken: cancellationToken).ConfigureAwait(false);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return sent;
        }
    }
}
