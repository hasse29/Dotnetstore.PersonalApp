using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Domain;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;

internal static class PhoneNumberTypeSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly PhoneNumberType[] All =
    [
        Create("0197c0de-0002-7000-8000-000000000001", "Mobile", "A mobile phone number."),
        Create("0197c0de-0002-7000-8000-000000000002", "Work", "A work phone number."),
        Create("0197c0de-0002-7000-8000-000000000003", "Home", "A private landline."),
    ];

    private static PhoneNumberType Create(string id, string name, string description) =>
        new() { Id = Guid.Parse(id), Name = name, Description = description, CreatedAt = SeededAt };
}