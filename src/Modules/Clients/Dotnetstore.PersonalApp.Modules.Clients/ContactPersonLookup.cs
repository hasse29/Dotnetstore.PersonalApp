using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;
using Dotnetstore.PersonalApp.Shared.Clients;

namespace Dotnetstore.PersonalApp.Modules.Clients;

internal sealed class ContactPersonLookup(ListContactPersons.Handler list) : IContactPersonLookup
{
    public async Task<IReadOnlyList<ContactPersonReference>> ListAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var people = await list.HandleAsync(new ListContactPersons.Query(clientId), cancellationToken).ConfigureAwait(false);
        return [.. people.Select(p => new ContactPersonReference(p.Id, p.FullName))];
    }
}
