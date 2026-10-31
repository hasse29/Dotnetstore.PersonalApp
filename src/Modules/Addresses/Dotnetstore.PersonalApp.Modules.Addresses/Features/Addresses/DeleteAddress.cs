using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

internal static class DeleteAddress
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<AddressesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Addresses
                .Where(a => a.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("address_not_found", "Address not found.") : command.Id;
        }
    }
}
