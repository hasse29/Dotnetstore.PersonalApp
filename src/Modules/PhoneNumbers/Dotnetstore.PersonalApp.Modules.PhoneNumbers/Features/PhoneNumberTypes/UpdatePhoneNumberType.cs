using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;

internal static class UpdatePhoneNumberType
{
    public sealed record Command(Guid Id, string Name, string? Description);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Description).DescriptionRules();
        }
    }

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<PhoneNumberTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.PhoneNumberTypes.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (type is null)
            {
                return Error.NotFound("phone_number_type_not_found", "Phone number type not found.");
            }

            var name = command.Name.Trim();
            if (await db.PhoneNumberTypes.AnyAsync(t => t.Name == name && t.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("phone_number_type_conflict", $"An phone number type named '{name}' already exists.");
            }

            type.Name = name;
            type.Description = PhoneNumberTypeRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return PhoneNumberTypeDto.From(type);
        }
    }
}
