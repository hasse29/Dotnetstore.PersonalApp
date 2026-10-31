using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Domain;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;

internal sealed record PhoneNumberTypeDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static PhoneNumberTypeDto From(PhoneNumberType type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}
