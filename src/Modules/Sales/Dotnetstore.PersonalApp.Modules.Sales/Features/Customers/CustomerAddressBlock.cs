using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Finds the address that is copied onto an offer. Addresses belong to the Contact module, so Sales asks for them through the shared contact sources.</summary>
internal static class CustomerAddressBlock
{
    /// <returns>The customer's first address (by type name) on separate lines, or null when it has none or no address source is installed.</returns>
    public static async Task<string?> ResolveAsync(IEnumerable<IContactInfoSource> sources, Guid customerId, CancellationToken cancellationToken)
    {
        // A source that only offers phone numbers or email addresses is not worth a query.
        foreach (var source in sources.Where(s => s.Kind is null or ContactKind.Address))
        {
            var entries = await source.ListAsync(customerId, cancellationToken).ConfigureAwait(false);
            if (entries.FirstOrDefault(e => e.Kind == ContactKind.Address) is { } address)
            {
                // Sources that cannot split an address leave Lines empty; their one-line form is split at the commas instead.
                var lines = address.Lines ?? address.Value.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return string.Join('\n', lines);
            }
        }

        return null;
    }
}
