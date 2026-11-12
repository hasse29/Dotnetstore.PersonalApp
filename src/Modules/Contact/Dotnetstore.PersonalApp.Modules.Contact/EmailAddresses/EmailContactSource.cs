using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses;

internal sealed class EmailContactSource(IDbContextFactory<ContactDbContext> dbFactory) : IContactInfoSource
{
    public ContactKind? Kind => ContactKind.Email;

    public async Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.EmailAddresses.AsNoTracking()
            .Where(e => e.ObjectId == objectId)
            .OrderBy(e => e.EmailAddressType.Name)
            .ThenBy(e => e.Email)
            .Select(e => new ContactEntry(e.Id, ContactKind.Email, e.EmailAddressType.Name, e.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
