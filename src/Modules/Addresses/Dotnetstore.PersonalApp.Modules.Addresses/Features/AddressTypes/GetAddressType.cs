using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

internal static class GetAddressType
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<AddressesDbContext> dbFactory)
    {
        public async Task<Result<AddressTypeDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.AddressTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("address_type_not_found", "Address type not found.") : AddressTypeDto.From(type);
        }
    }
}
