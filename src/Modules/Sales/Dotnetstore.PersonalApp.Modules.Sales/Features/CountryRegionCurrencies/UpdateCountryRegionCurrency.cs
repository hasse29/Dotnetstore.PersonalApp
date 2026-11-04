using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;

internal static class UpdateCountryRegionCurrency
{
    public sealed record Command(Guid Id, string CountryRegion, Guid CurrencyId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.CountryRegion).NotEmpty().WithErrorCode("country_region_currency_country_region_required").WithMessage("Country or region is required.").MaximumLength(100).WithErrorCode("country_region_currency_country_region_too_long").WithMessage("Country or region is too long.");
            RuleFor(c => c.CurrencyId).NotEqual(Guid.Empty).WithErrorCode("country_region_currency_currency_required").WithMessage("Currency is required.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<CountryRegionCurrencyDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var countryRegionValue = command.CountryRegion.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.CountryRegionCurrencies.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("country_region_currency_not_found", "The record was not found.");
            }

            if (!await db.Currencies.AnyAsync(x => x.Id == command.CurrencyId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("country_region_currency_currency_not_found", "Currency not found.");
            }

            if (await db.CountryRegionCurrencies.AnyAsync(x => x.CountryRegion == countryRegionValue && x.CurrencyId == command.CurrencyId && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("country_region_currency_conflict", "A country currency with the same values already exists.");
            }

            entity.CountryRegion = countryRegionValue;
            entity.CurrencyId = command.CurrencyId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.CountryRegionCurrencies.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(CountryRegionCurrencyDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}