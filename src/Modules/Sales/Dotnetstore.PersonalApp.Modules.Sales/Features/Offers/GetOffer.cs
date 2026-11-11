using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal static class GetOffer
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<OfferDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offer = await db.Offers.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken).ConfigureAwait(false);

            return offer is null
                ? Error.NotFound("offer_not_found", "Offer not found.")
                : OfferDetailDto.From(offer, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        }
    }
}
