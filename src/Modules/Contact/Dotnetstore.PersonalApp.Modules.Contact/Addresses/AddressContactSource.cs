using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses;

internal sealed class AddressContactSource(IDbContextFactory<ContactDbContext> dbFactory) : IContactInfoSource
{
    public ContactKind? Kind => ContactKind.Address;

    public async Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var addresses = await db.Addresses.AsNoTracking()
            .Include(a => a.AddressType)
            .Where(a => a.ObjectId == objectId)
            .OrderBy(a => a.AddressType.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. addresses.Select(a =>
        {
            string[] lines = [.. new[] { a.CareOf, a.Street1, a.Street2, $"{a.PostalCode} {a.City}".Trim(), a.State, a.Country }.Where(p => !string.IsNullOrWhiteSpace(p))!];
            return new ContactEntry(a.Id, ContactKind.Address, a.AddressType.Name, string.Join(", ", lines), lines);
        })];
    }
}
