using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Stands in for the Addresses module: returns the address set for an object, if any.</summary>
internal sealed class FakeContactSource : IContactInfoSource
{
    private readonly Dictionary<Guid, string> _addresses = [];

    public void SetAddress(Guid objectId, string address) => _addresses[objectId] = address;

    public void RemoveAddress(Guid objectId) => _addresses.Remove(objectId);

    public Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ContactEntry>>(_addresses.TryGetValue(objectId, out var address)
            ? [new ContactEntry(Guid.NewGuid(), ContactKind.Address, "Work", address)]
            : []);
}
