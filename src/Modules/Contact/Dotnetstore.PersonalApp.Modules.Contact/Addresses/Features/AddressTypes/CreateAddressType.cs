using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

internal static class CreateAddressType
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
        public async Task<Result<AddressTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.AddressTypes.AnyAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("address_type_conflict", $"An address type named '{name}' already exists.");
            }

            var type = new AddressType
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Description = AddressTypeRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
            };

            db.AddressTypes.Add(type);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AddressTypeDto.From(type);
        }
    }
}
