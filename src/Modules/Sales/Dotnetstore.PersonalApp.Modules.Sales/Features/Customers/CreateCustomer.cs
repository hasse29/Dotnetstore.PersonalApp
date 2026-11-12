using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class CreateCustomer
{
    public sealed record Command(CustomerFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.OrganizationNumber).OrganizationNumberRules();
            RuleFor(c => c.Fields.Notes).NotesRules();
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<CustomerDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<CustomerDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Fields.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Customers.AnyAsync(c => c.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_name_conflict", $"A customer named '{name}' already exists.");
            }

            var customer = new Customer { Id = Guid.CreateVersion7(), CreatedAt = clock.GetUtcNow() };
            CustomerRules.Apply(customer, command.Fields);

            db.Customers.Add(customer);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CustomerDto.From(customer);
        }
    }
}
