namespace Dotnetstore.PersonalApp.Modules.Addresses.Domain;

/// <summary>A user-managed kind of address, e.g. Billing or Delivery.</summary>
internal sealed class AddressType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
