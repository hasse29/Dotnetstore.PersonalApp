using Dotnetstore.PersonalApp.Modules.Addresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Persistence;

internal static class AddressTypeSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly AddressType[] All =
    [
        Create("0197c0de-0000-7000-8000-000000000001", "Billing", "Where invoices are sent."),
        Create("0197c0de-0000-7000-8000-000000000002", "Delivery", "Where goods are delivered."),
        Create("0197c0de-0000-7000-8000-000000000003", "Visiting", "Where to meet in person."),
        Create("0197c0de-0000-7000-8000-000000000004", "Home", "A private home address."),
    ];

    private static AddressType Create(string id, string name, string description) =>
        new() { Id = Guid.Parse(id), Name = name, Description = description, CreatedAt = SeededAt };
}
