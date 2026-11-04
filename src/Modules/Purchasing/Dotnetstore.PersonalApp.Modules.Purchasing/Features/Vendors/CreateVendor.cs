using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal static class CreateVendor
{
    public sealed record Command(string AccountNumber, string Name, int CreditRating, bool PreferredVendorStatus, bool ActiveFlag, string? PurchasingWebServiceUrl, Guid? ClientId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.AccountNumber).NotEmpty().WithErrorCode("vendor_account_number_required").WithMessage("Account number is required.").MaximumLength(15).WithErrorCode("vendor_account_number_too_long").WithMessage("Account number is too long.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("vendor_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("vendor_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.CreditRating).InclusiveBetween(1, 5).WithErrorCode("vendor_credit_rating_out_of_range").WithMessage("Credit rating (1-5) is out of range.");
            RuleFor(c => c.PurchasingWebServiceUrl).MaximumLength(1024).WithErrorCode("vendor_purchasing_web_service_url_too_long").WithMessage("Web address is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<VendorDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var accountNumberValue = command.AccountNumber.Trim();
            var nameValue = command.Name.Trim();
            var purchasingWebServiceUrlValue = PurchasingRules.NormalizeOptional(command.PurchasingWebServiceUrl);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (command.ClientId is { } clientId && !await references.ClientExistsAsync(clientId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("vendor_client_not_found", "Linked client not found.");
            }

            if (await db.Vendors.AnyAsync(x => x.AccountNumber == accountNumberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("vendor_conflict", "A vendor with the same values already exists.");
            }

            if (await db.Vendors.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("vendor_conflict", "A vendor with the same values already exists.");
            }

            var entity = new Vendor
            {
                Id = Guid.CreateVersion7(),
                AccountNumber = accountNumberValue,
                Name = nameValue,
                CreditRating = command.CreditRating,
                PreferredVendorStatus = command.PreferredVendorStatus,
                ActiveFlag = command.ActiveFlag,
                PurchasingWebServiceUrl = purchasingWebServiceUrlValue,
                ClientId = command.ClientId,
            };

            db.Vendors.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.Vendors.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(VendorDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ClientNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}