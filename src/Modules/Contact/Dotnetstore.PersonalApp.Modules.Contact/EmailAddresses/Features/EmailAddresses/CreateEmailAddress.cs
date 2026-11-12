using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

internal static class CreateEmailAddress
{
    public sealed record Command(Guid ObjectId, Guid EmailAddressTypeId, string Email);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ObjectId).ObjectIdRules();
            RuleFor(c => c.EmailAddressTypeId).EmailAddressTypeIdRules();
            RuleFor(c => c.Email).EmailRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<EmailAddressDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var typeName = await db.EmailAddressTypes
                .Where(t => t.Id == command.EmailAddressTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("email_address_type_not_found", "Email address type not found.");
            }

            var email = command.Email.Trim();
            if (await db.EmailAddresses.AnyAsync(e => e.ObjectId == command.ObjectId && e.Email == email, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_address_conflict", $"'{email}' is already registered for this object.");
            }

            var address = new EmailAddress
            {
                Id = Guid.CreateVersion7(),
                ObjectId = command.ObjectId,
                EmailAddressTypeId = command.EmailAddressTypeId,
                Email = email,
            };

            db.EmailAddresses.Add(address);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAddressDto.From(address, typeName);
        }
    }
}