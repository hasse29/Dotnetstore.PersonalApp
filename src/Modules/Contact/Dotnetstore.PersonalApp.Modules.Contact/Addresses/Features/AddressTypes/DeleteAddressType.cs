using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

internal static class DeleteAddressType
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Addresses.AnyAsync(a => a.AddressTypeId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("address_type_in_use", "The address type is used by existing addresses.");
            }

            var deleted = await db.AddressTypes
                .Where(t => t.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("address_type_not_found", "Address type not found.") : command.Id;
        }
    }
}
