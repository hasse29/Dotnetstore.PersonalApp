using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

/// <summary>Changes an email address. The owning object is fixed; to move one, delete it and create a new one.</summary>
internal static class UpdateEmailAddress
{
    public sealed record Command(Guid Id, Guid EmailAddressTypeId, string Email);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.EmailAddressTypeId).EmailAddressTypeIdRules();
            RuleFor(c => c.Email).EmailRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<EmailAddressDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<EmailAddressDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var address = await db.EmailAddresses.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (address is null)
            {
                return Error.NotFound("email_address_not_found", "Email address not found.");
            }

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
            if (await db.EmailAddresses.AnyAsync(
                    e => e.ObjectId == address.ObjectId && e.Email == email && e.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_address_conflict", $"'{email}' is already registered for this object.");
            }

            address.EmailAddressTypeId = command.EmailAddressTypeId;
            address.Email = email;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAddressDto.From(address, typeName);
        }
    }
}