using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;

internal static class CreateEmailAddressType
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
        public async Task<Result<EmailAddressTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.EmailAddressTypes.AnyAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_address_type_conflict", $"An email address type named '{name}' already exists.");
            }

            var type = new EmailAddressType
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Description = EmailAddressTypeRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
            };

            db.EmailAddressTypes.Add(type);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAddressTypeDto.From(type);
        }
    }
}
