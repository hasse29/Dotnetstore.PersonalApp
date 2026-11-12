namespace Dotnetstore.PersonalApp.Shared.Contacts;

public enum ContactKind
{
    Address,
    Phone,
    Email,
}

/// <summary>One way to reach an object (a user, ...), as another module may show it.</summary>
/// <param name="Id">The id of the address, phone number or email address in the module that owns it.</param>
/// <param name="TypeName">The user-managed type, e.g. "Work" or "Mobile".</param>
/// <param name="Value">The address on one line, the phone number as typed or the email address.</param>
/// <param name="Lines">An address split into its lines (care of, street, postal code and city, ...) for documents that print it on several lines. Null for phone numbers and email addresses.</param>
public sealed record ContactEntry(Guid Id, ContactKind Kind, string TypeName, string Value, IReadOnlyList<string>? Lines = null);

/// <summary>
/// Implemented by the Contact module (it registers one per kind: addresses, phone numbers, email addresses) so that a module
/// can show an object's contact details without referencing it.
/// </summary>
public interface IContactInfoSource
{
    /// <summary>The only kind of entry this source returns, so a caller that wants one kind can skip the others. Null when it returns several.</summary>
    ContactKind? Kind => null;

    Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken);
}
