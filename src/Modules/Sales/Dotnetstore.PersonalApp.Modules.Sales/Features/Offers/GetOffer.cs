using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal static class GetOffer
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<OfferDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<OfferDetailDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offer = await db.Offers.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken).ConfigureAwait(false);

            if (offer is null)
            {
                return Error.NotFound("offer_not_found", "Offer not found.");
            }

            // The order the offer was made into, if there is a live one.
            var orderNumber = await db.Orders.AsNoTracking()
                .Where(o => o.OfferId == offer.Id && o.Status != OrderStatus.Cancelled)
                .Select(o => o.Number)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return OfferDetailDto.From(offer, DateOnly.FromDateTime(clock.GetLocalNow().DateTime)) with { OrderNumber = orderNumber };
        }
    }
}
