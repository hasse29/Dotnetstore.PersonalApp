using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;

internal static class EmailAddressTypeSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly EmailAddressType[] All =
    [
        Create("0197c0de-0001-7000-8000-000000000001", "Work", "A work email address."),
        Create("0197c0de-0001-7000-8000-000000000002", "Private", "A private email address."),
        Create("0197c0de-0001-7000-8000-000000000003", "Billing", "Where invoices are sent."),
    ];

    private static EmailAddressType Create(string id, string name, string description) =>
        new() { Id = Guid.Parse(id), Name = name, Description = description, CreatedAt = SeededAt };
}