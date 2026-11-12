using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;

internal static class CreateAddress
{
    public sealed record Command(
        Guid ObjectId,
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
            RuleFor(c => c.ObjectId).ObjectIdRules();
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

            var typeName = await db.AddressTypes
                .Where(t => t.Id == command.AddressTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (typeName is null)
            {
                return Error.NotFound("address_type_not_found", "Address type not found.");
            }

            var address = new Address
            {
                Id = Guid.CreateVersion7(),
                ObjectId = command.ObjectId,
                AddressTypeId = command.AddressTypeId,
                CareOf = AddressRules.NormalizeOptional(command.CareOf),
                Street1 = command.Street1.Trim(),
                Street2 = AddressRules.NormalizeOptional(command.Street2),
                PostalCode = command.PostalCode.Trim(),
                City = command.City.Trim(),
                State = AddressRules.NormalizeOptional(command.State),
                Country = command.Country.Trim(),
            };

            db.Addresses.Add(address);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AddressDto.From(address, typeName);
        }
    }
}
