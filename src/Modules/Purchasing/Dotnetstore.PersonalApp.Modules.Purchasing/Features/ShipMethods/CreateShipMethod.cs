using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal static class CreateShipMethod
{
    public sealed record Command(string Name, decimal ShipBase, decimal ShipRate);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("ship_method_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("ship_method_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.ShipBase).InclusiveBetween(0m, 1000000000m).WithErrorCode("ship_method_ship_base_out_of_range").WithMessage("Base charge is out of range.");
            RuleFor(c => c.ShipRate).InclusiveBetween(0m, 1000000000m).WithErrorCode("ship_method_ship_rate_out_of_range").WithMessage("Rate per unit is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ShipMethodDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.ShipMethods.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("ship_method_conflict", "A ship method with the same values already exists.");
            }

            var entity = new ShipMethod
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                ShipBase = command.ShipBase,
                ShipRate = command.ShipRate,
            };

            db.ShipMethods.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ShipMethods.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ShipMethodDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}