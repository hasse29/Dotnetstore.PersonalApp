using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal static class UpdateSupplier
{
    public sealed record Command(Guid Id, SupplierFields Fields);

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

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator)
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

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (supplier is null)
            {
                return Error.NotFound("supplier_not_found", "Supplier not found.");
            }

            var name = command.Fields.Name.Trim();
            if (await db.Suppliers.AnyAsync(s => s.Name == name && s.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("supplier_name_conflict", $"A supplier named '{name}' already exists.");
            }

            SupplierRules.Apply(supplier, command.Fields);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return SupplierDto.From(supplier);
        }
    }
}
