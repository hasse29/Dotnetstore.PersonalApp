using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;

internal static class DeletePhoneNumberType
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.PhoneNumbers.AnyAsync(a => a.PhoneNumberTypeId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("phone_number_type_in_use", "The phone number type is used by existing addresses.");
            }

            var deleted = await db.PhoneNumberTypes
                .Where(t => t.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("phone_number_type_not_found", "Phone number type not found.") : command.Id;
        }
    }
}
