using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal static class CreateStorageLocation
{
    public sealed record Command(StorageLocationFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.Description).DescriptionRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<StorageLocationDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<StorageLocationDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Fields.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.StorageLocations.AnyAsync(l => l.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("storage_location_conflict", $"A storage location named '{name}' already exists.");
            }

            var location = new StorageLocation { Id = Guid.CreateVersion7(), CreatedAt = clock.GetUtcNow() };
            StorageLocationRules.Apply(location, command.Fields);

            db.StorageLocations.Add(location);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StorageLocationDto.From(location);
        }
    }
}
