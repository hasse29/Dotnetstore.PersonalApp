using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;

/// <summary>Changes an address. The owning object is fixed; to move an address, delete it and create a new one.</summary>
internal static class UpdateAddress
{
    public sealed record Command(
        Guid Id,
        Guid AddressTypeId,
        string? CareOf,
        string Street1,
        string? Street2,
        string PostalCode,
        string City,
        string? State,
        string Country);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.AddressTypeId).AddressTypeIdRules();
            RuleFor(c => c.CareOf).CareOfRules();
            RuleFor(c => c.Street1).Street1Rules();
            RuleFor(c => c.Street2).Street2Rules();
            RuleFor(c => c.PostalCode).PostalCodeRules();
            RuleFor(c => c.City).CityRules();
            RuleFor(c => c.State).StateRules();
            RuleFor(c => c.Country).CountryRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<AddressDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<AddressDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (address is null)
            {
                return Error.NotFound("address_not_found", "Address not found.");
            }

            var typeName = await db.AddressTypes
                .Where(t => t.Id == command.AddressTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("address_type_not_found", "Address type not found.");
            }

            address.AddressTypeId = command.AddressTypeId;
            address.CareOf = AddressRules.NormalizeOptional(command.CareOf);
            address.Street1 = command.Street1.Trim();
            address.Street2 = AddressRules.NormalizeOptional(command.Street2);
            address.PostalCode = command.PostalCode.Trim();
            address.City = command.City.Trim();
            address.State = AddressRules.NormalizeOptional(command.State);
            address.Country = command.Country.Trim();

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AddressDto.From(address, typeName);
        }
    }
}
