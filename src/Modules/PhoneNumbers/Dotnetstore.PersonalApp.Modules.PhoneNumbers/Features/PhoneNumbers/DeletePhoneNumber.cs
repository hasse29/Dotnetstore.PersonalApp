using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal static class DeletePhoneNumber
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.PhoneNumbers
                .Where(a => a.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("phone_number_not_found", "Phone number not found.") : command.Id;
        }
    }
}
