using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

internal static class GetAddress
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<AddressesDbContext> dbFactory)
    {
        public async Task<Result<AddressDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var address = await db.Addresses.AsNoTracking()
                .Include(a => a.AddressType)
                .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return address is null
                ? Error.NotFound("address_not_found", "Address not found.")
                : AddressDto.From(address, address.AddressType.Name);
        }
    }
}
