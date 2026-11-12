using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;

/// <summary>Changes a phone number. The owning object is fixed; to move one, delete it and create a new one.</summary>
internal static class UpdatePhoneNumber
{
    public sealed record Command(Guid Id, Guid PhoneNumberTypeId, string Number);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.PhoneNumberTypeId).PhoneNumberTypeIdRules();
            RuleFor(c => c.Number).NumberRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<PhoneNumberDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var phone = await db.PhoneNumbers.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (phone is null)
            {
                return Error.NotFound("phone_number_not_found", "Phone number not found.");
            }

            var typeName = await db.PhoneNumberTypes
                .Where(t => t.Id == command.PhoneNumberTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("phone_number_type_not_found", "Phone number type not found.");
            }

            var number = command.Number.Trim();
            var normalized = PhoneNumberRules.Normalize(number);
            if (await db.PhoneNumbers.AnyAsync(
                    p => p.ObjectId == phone.ObjectId && p.NormalizedNumber == normalized && p.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("phone_number_conflict", $"'{number}' is already registered for this object.");
            }

            phone.PhoneNumberTypeId = command.PhoneNumberTypeId;
            phone.Number = number;
            phone.NormalizedNumber = normalized;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return PhoneNumberDto.From(phone, typeName);
        }
    }
}