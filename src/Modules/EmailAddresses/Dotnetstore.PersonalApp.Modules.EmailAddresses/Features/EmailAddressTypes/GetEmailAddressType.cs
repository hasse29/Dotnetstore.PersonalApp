using Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;

internal static class GetEmailAddressType
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<EmailAddressesDbContext> dbFactory)
    {
        public async Task<Result<EmailAddressTypeDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.EmailAddressTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("email_address_type_not_found", "Email address type not found.") : EmailAddressTypeDto.From(type);
        }
    }
}
