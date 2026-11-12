using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class UpdateCustomer
{
    public sealed record Command(Guid Id, CustomerFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.OrganizationNumber).OrganizationNumberRules();
            RuleFor(c => c.Fields.Notes).NotesRules();
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
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

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (customer is null)
            {
                return Error.NotFound("customer_not_found", "Customer not found.");
            }

            var name = command.Fields.Name.Trim();
            if (await db.Customers.AnyAsync(c => c.Name == name && c.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_name_conflict", $"A customer named '{name}' already exists.");
            }

            CustomerRules.Apply(customer, command.Fields);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CustomerDto.From(customer);
        }
    }
}
