using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class CreateCustomer
{
    public sealed record Command(string? Number, string Name, Guid? ClientId, Guid? StoreId, Guid? TerritoryId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).MaximumLength(25).WithErrorCode("customer_number_too_long").WithMessage("Customer number is too long.");
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

            var numberValue = SalesRules.NormalizeOptional(command.Number);
            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            numberValue ??= await NextNumberAsync(db, cancellationToken).ConfigureAwait(false);

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

            if (await db.Customers.AnyAsync(x => x.Number == numberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_conflict", "A customer with the same values already exists.");
            }

            var entity = new Customer
            {
                Id = Guid.CreateVersion7(),
                Number = numberValue,
                Name = nameValue,
                ClientId = command.ClientId,
                StoreId = command.StoreId,
                TerritoryId = command.TerritoryId,
            };

            db.Customers.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.Customers.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(CustomerDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ClientNamesAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<string> NextNumberAsync(SalesDbContext db, CancellationToken cancellationToken)
        {
            var count = await db.Customers.CountAsync(cancellationToken).ConfigureAwait(false);
            string candidate;
            do
            {
                count++;
                candidate = $"CU-{count:D6}";
            }
            while (await db.Customers.AnyAsync(x => x.Number == candidate, cancellationToken).ConfigureAwait(false));

            return candidate;
        }
    }
}