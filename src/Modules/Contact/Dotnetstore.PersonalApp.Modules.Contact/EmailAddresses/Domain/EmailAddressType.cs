namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Domain;

/// <summary>A user-managed kind of address, e.g. Billing or Delivery.</summary>
internal sealed class EmailAddressType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
