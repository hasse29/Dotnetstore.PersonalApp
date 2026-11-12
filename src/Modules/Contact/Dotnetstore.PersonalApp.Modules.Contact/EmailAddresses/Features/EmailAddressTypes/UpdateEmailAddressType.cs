using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;

internal static class UpdateEmailAddressType
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

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<EmailAddressTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.EmailAddressTypes.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (type is null)
            {
                return Error.NotFound("email_address_type_not_found", "Email address type not found.");
            }

            var name = command.Name.Trim();
            if (await db.EmailAddressTypes.AnyAsync(t => t.Name == name && t.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_address_type_conflict", $"An email address type named '{name}' already exists.");
            }

            type.Name = name;
            type.Description = EmailAddressTypeRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAddressTypeDto.From(type);
        }
    }
}
