using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

internal static class DeleteCurrency
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.CurrencyRates.AnyAsync(x => x.FromCurrencyId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.CurrencyRates.AnyAsync(x => x.ToCurrencyId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.CountryRegionCurrencies.AnyAsync(x => x.CurrencyId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.Currencies
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("currency_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}