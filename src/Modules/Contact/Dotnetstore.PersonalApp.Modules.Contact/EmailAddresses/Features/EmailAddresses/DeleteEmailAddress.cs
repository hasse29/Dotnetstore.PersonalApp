using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

internal static class DeleteEmailAddress
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.EmailAddresses
                .Where(a => a.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("email_address_not_found", "Email address not found.") : command.Id;
        }
    }
}
