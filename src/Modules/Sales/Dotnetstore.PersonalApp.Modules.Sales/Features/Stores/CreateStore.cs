using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;

internal static class CreateStore
{
    public sealed record Command(string Name, Guid? SalesPersonId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("store_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("store_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<StoreDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (command.SalesPersonId is { } salesPersonId && !await db.SalesPeople.AnyAsync(x => x.Id == salesPersonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("store_sales_person_not_found", "Salesperson not found.");
            }

            if (await db.Stores.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("store_conflict", "A store with the same values already exists.");
            }

            var entity = new Store
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                SalesPersonId = command.SalesPersonId,
            };

            db.Stores.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.Stores.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(StoreDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}