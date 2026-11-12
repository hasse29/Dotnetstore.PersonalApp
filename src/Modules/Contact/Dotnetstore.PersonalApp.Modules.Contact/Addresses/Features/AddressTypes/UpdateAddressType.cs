using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

internal static class UpdateAddressType
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
        public Task<Result<AddressTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<AddressTypeDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.AddressTypes.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (type is null)
            {
                return Error.NotFound("address_type_not_found", "Address type not found.");
            }

            var name = command.Name.Trim();
            if (await db.AddressTypes.AnyAsync(t => t.Name == name && t.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("address_type_conflict", $"An address type named '{name}' already exists.");
            }

            type.Name = name;
            type.Description = AddressTypeRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AddressTypeDto.From(type);
        }
    }
}
