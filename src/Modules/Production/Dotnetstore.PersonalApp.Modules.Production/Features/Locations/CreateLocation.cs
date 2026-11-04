using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Locations;

internal static class CreateLocation
{
    public sealed record Command(string Name, decimal CostRate, decimal Availability);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("location_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("location_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.CostRate).InclusiveBetween(0m, 1000000000m).WithErrorCode("location_cost_rate_out_of_range").WithMessage("Cost rate per hour is out of range.");
            RuleFor(c => c.Availability).InclusiveBetween(0m, 1000m).WithErrorCode("location_availability_out_of_range").WithMessage("Available hours is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<LocationDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.Locations.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("location_conflict", "A production location with the same values already exists.");
            }

            var entity = new Location
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                CostRate = command.CostRate,
                Availability = command.Availability,
            };

            db.Locations.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.Locations.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(LocationDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}