using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal static class CreateSupplier
{
    public sealed record Command(SupplierFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.OrganizationNumber).OrganizationNumberRules();
            RuleFor(c => c.Fields.Email).EmailRules();
            RuleFor(c => c.Fields.Phone).PhoneRules();
            RuleFor(c => c.Fields.Website).WebsiteRules();
            RuleFor(c => c.Fields.Notes).NotesRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<SupplierDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<SupplierDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Fields.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Suppliers.AnyAsync(s => s.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("supplier_name_conflict", $"A supplier named '{name}' already exists.");
            }

            var supplier = new Supplier { Id = Guid.CreateVersion7(), CreatedAt = clock.GetUtcNow() };
            SupplierRules.Apply(supplier, command.Fields);

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return SupplierDto.From(supplier);
        }
    }
}
