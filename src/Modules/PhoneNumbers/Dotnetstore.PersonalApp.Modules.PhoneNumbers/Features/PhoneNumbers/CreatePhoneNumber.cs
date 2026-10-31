using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Domain;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal static class CreatePhoneNumber
{
    public sealed record Command(Guid ObjectId, Guid PhoneNumberTypeId, string Number);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ObjectId).ObjectIdRules();
            RuleFor(c => c.PhoneNumberTypeId).PhoneNumberTypeIdRules();
            RuleFor(c => c.Number).NumberRules();
        }
    }

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<PhoneNumberDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

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
            if (await db.PhoneNumbers.AnyAsync(p => p.ObjectId == command.ObjectId && p.NormalizedNumber == normalized, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("phone_number_conflict", $"'{number}' is already registered for this object.");
            }

            var phone = new PhoneNumber
            {
                Id = Guid.CreateVersion7(),
                ObjectId = command.ObjectId,
                PhoneNumberTypeId = command.PhoneNumberTypeId,
                Number = number,
                NormalizedNumber = normalized,
            };

            db.PhoneNumbers.Add(phone);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return PhoneNumberDto.From(phone, typeName);
        }
    }
}