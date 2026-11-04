using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class UpdateCustomer
{
    public sealed record Command(Guid Id, string Number, string Name, Guid? ClientId, Guid? StoreId, Guid? TerritoryId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).NotEmpty().WithErrorCode("customer_number_required").WithMessage("Customer number is required.").MaximumLength(25).WithErrorCode("customer_number_too_long").WithMessage("Customer number is too long.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("customer_name_required").WithMessage("Name is required.").MaximumLength(100).WithErrorCode("customer_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<CustomerDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var numberValue = command.Number.Trim();
            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.Customers.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("customer_not_found", "The record was not found.");
            }

            if (command.ClientId is { } clientId && !await references.ClientExistsAsync(clientId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("customer_client_not_found", "Linked client not found.");
            }

            if (command.StoreId is { } storeId && !await db.Stores.AnyAsync(x => x.Id == storeId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("customer_store_not_found", "Store not found.");
            }

            if (command.TerritoryId is { } territoryId && !await db.SalesTerritories.AnyAsync(x => x.Id == territoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("customer_territory_not_found", "Territory not found.");
            }

            if (await db.Customers.AnyAsync(x => x.Number == numberValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_conflict", "A customer with the same values already exists.");
            }

            entity.Number = numberValue;
            entity.Name = nameValue;
            entity.ClientId = command.ClientId;
            entity.StoreId = command.StoreId;
            entity.TerritoryId = command.TerritoryId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.Customers.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(CustomerDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ClientNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}