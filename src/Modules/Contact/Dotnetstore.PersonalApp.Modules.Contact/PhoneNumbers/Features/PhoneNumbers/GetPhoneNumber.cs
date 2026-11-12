using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;

internal static class GetPhoneNumber
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Result<PhoneNumberDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<PhoneNumberDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var address = await db.PhoneNumbers.AsNoTracking()
                .Include(a => a.PhoneNumberType)
                .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return address is null
                ? Error.NotFound("phone_number_not_found", "Phone number not found.")
                : PhoneNumberDto.From(address, address.PhoneNumberType.Name);
        }
    }
}
