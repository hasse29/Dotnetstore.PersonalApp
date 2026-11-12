using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

internal static class GetAddressType
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<AddressTypeDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<AddressTypeDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.AddressTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("address_type_not_found", "Address type not found.") : AddressTypeDto.From(type);
        }
    }
}
