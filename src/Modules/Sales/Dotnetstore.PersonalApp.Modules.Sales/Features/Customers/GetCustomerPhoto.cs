using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Reads only the photo, so listing customers never drags their pictures along.</summary>
internal static class GetCustomerPhoto
{
    public sealed record Query(Guid CustomerId);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<byte[]?>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await db.Customers.AsNoTracking()
                .Where(x => x.Id == query.CustomerId)
                .Select(x => new { x.Photo })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return found is null ? Error.NotFound("customer_not_found", "Customer not found.") : Result<byte[]?>.Success(found.Photo);
        }
    }
}
