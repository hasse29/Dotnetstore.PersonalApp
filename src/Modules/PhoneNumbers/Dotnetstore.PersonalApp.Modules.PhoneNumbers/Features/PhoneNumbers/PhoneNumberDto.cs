using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Domain;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal sealed record PhoneNumberDto(Guid Id, Guid ObjectId, Guid PhoneNumberTypeId, string PhoneNumberTypeName, string Number)
{
    public static PhoneNumberDto From(PhoneNumber phone, string typeName) =>
        new(phone.Id, phone.ObjectId, phone.PhoneNumberTypeId, typeName, phone.Number);
}