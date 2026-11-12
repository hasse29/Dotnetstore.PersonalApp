using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>
/// Moves an offer along: Draft to Sent (needs at least one line), Sent to Accepted or Declined, and back to Draft from Sent or
/// Declined so it can be changed. An accepted offer stays accepted: it is what an order is made from.
/// </summary>
internal static class ChangeOfferStatus
{
    public sealed record Command(Guid Id, OfferStatus Target);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<OfferDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<OfferDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offer = await db.Offers.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (offer is null)
            {
                return Error.NotFound("offer_not_found", "Offer not found.");
            }

            if (!IsAllowed(offer.Status, command.Target))
            {
                return Error.Conflict("offer_status_invalid", $"An offer cannot go from {offer.Status} to {command.Target}.");
            }

            if (command.Target == OfferStatus.Sent && offer.Lines.Count == 0)
            {
                return Error.Validation("offer_no_lines", "Add at least one line before sending the offer.");
            }

            offer.Status = command.Target;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return OfferDetailDto.From(offer, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        }

        private static bool IsAllowed(OfferStatus from, OfferStatus to) => (from, to) switch
        {
            (OfferStatus.Draft, OfferStatus.Sent) => true,
            (OfferStatus.Sent, OfferStatus.Accepted) => true,
            (OfferStatus.Sent, OfferStatus.Declined) => true,
            (OfferStatus.Sent, OfferStatus.Draft) => true,
            (OfferStatus.Declined, OfferStatus.Draft) => true,
            _ => false,
        };
    }
}
