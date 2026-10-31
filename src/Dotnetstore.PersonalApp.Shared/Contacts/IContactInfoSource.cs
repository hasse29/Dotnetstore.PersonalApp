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
public sealed record ContactEntry(Guid Id, ContactKind Kind, string TypeName, string Value);

/// <summary>
/// Implemented by the Addresses, PhoneNumbers and EmailAddresses modules (each registers one) so that a module can show an
/// object's contact details without referencing them.
/// </summary>
public interface IContactInfoSource
{
    Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken);
}
