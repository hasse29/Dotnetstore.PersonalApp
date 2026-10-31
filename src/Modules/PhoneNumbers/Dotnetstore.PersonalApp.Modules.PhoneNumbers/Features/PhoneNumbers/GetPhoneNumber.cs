using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal static class GetPhoneNumber
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory)
    {
        public async Task<Result<PhoneNumberDto>> HandleAsync(Query query, CancellationToken cancellationToken)
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
