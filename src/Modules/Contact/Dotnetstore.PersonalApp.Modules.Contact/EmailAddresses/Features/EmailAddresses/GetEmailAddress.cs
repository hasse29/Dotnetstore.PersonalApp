using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

internal static class GetEmailAddress
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<EmailAddressDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<EmailAddressDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var address = await db.EmailAddresses.AsNoTracking()
                .Include(a => a.EmailAddressType)
                .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return address is null
                ? Error.NotFound("email_address_not_found", "Email address not found.")
                : EmailAddressDto.From(address, address.EmailAddressType.Name);
        }
    }
}
