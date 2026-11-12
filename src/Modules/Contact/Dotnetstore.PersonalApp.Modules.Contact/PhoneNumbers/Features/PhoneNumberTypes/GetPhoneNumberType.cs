using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;

internal static class GetPhoneNumberType
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public async Task<Result<PhoneNumberTypeDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.PhoneNumberTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("phone_number_type_not_found", "Phone number type not found.") : PhoneNumberTypeDto.From(type);
        }
    }
}
