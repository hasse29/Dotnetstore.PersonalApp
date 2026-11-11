using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal static class UpdateStorageLocation
{
    public sealed record Command(Guid Id, StorageLocationFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.Description).DescriptionRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<StorageLocationDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var location = await db.StorageLocations.FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (location is null)
            {
                return Error.NotFound("storage_location_not_found", "Storage location not found.");
            }

            var name = command.Fields.Name.Trim();
            if (await db.StorageLocations.AnyAsync(l => l.Name == name && l.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("storage_location_conflict", $"A storage location named '{name}' already exists.");
            }

            StorageLocationRules.Apply(location, command.Fields);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StorageLocationDto.From(location);
        }
    }
}
