using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;

internal static class CreatePhoneNumberType
{
    public sealed record Command(string Name, string? Description);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Description).DescriptionRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<ContactDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock)
    {
        public Task<Result<PhoneNumberTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<PhoneNumberTypeDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.PhoneNumberTypes.AnyAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("phone_number_type_conflict", $"An phone number type named '{name}' already exists.");
            }

            var type = new PhoneNumberType
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Description = PhoneNumberTypeRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
            };

            db.PhoneNumberTypes.Add(type);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return PhoneNumberTypeDto.From(type);
        }
    }
}
