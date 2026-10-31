using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers;

internal sealed class PhoneContactSource(IDbContextFactory<PhoneNumbersDbContext> dbFactory) : IContactInfoSource
{
    public async Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.PhoneNumbers.AsNoTracking()
            .Where(p => p.ObjectId == objectId)
            .OrderBy(p => p.PhoneNumberType.Name)
            .ThenBy(p => p.Number)
            .Select(p => new ContactEntry(p.Id, ContactKind.Phone, p.PhoneNumberType.Name, p.Number))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
