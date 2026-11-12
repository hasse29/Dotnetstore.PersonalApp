using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

/// <summary>Deletes every address of one object. Having no addresses is not an error: the result is the number deleted.</summary>
internal static class DeleteEmailAddressesForObject
{
    public sealed record Command(Guid ObjectId);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<int>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.ObjectId == Guid.Empty)
            {
                return Error.Validation("email_address_object_required", "The address must belong to an object.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.EmailAddresses
                .Where(a => a.ObjectId == command.ObjectId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
