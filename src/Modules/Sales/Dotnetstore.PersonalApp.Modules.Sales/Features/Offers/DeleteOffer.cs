using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>Deletes a draft or declined offer with its lines. A sent or accepted offer has been seen by the customer and stays.</summary>
internal static class DeleteOffer
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var status = await db.Offers
                .Where(o => o.Id == command.Id)
                .Select(o => (OfferStatus?)o.Status)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (status is null)
            {
                return Error.NotFound("offer_not_found", "Offer not found.");
            }

            if (status is not (OfferStatus.Draft or OfferStatus.Declined))
            {
                return Error.Conflict("offer_not_deletable", "Only a draft or declined offer can be deleted.");
            }

            await db.Offers.Where(o => o.Id == command.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}
