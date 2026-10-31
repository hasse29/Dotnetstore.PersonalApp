using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal static class ContactEntries
{
    /// <summary>
    /// Everything the user's contact modules offer, grouped addresses first, then phone numbers, then email addresses.
    /// A source that is not installed simply contributes nothing.
    /// </summary>
    public static async Task<IReadOnlyList<ContactEntry>> ListAsync(
        IEnumerable<IContactInfoSource> sources, Guid userId, CancellationToken cancellationToken)
    {
        var all = new List<ContactEntry>();
        foreach (var source in sources)
        {
            all.AddRange(await source.ListAsync(userId, cancellationToken).ConfigureAwait(false));
        }

        return [.. all.OrderBy(e => e.Kind).ThenBy(e => e.TypeName, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Value, StringComparer.OrdinalIgnoreCase)];
    }
}
