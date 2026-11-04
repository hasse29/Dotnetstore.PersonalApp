using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;

internal static class CreateCurrencyRate
{
    public sealed record Command(DateOnly RateDate, Guid FromCurrencyId, Guid ToCurrencyId, decimal AverageRate, decimal EndOfDayRate);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.FromCurrencyId).NotEqual(Guid.Empty).WithErrorCode("currency_rate_from_currency_required").WithMessage("From currency is required.");
            RuleFor(c => c.ToCurrencyId).NotEqual(Guid.Empty).WithErrorCode("currency_rate_to_currency_required").WithMessage("To currency is required.");
            RuleFor(c => c.AverageRate).InclusiveBetween(0m, 1000000000m).WithErrorCode("currency_rate_average_rate_out_of_range").WithMessage("Average rate is out of range.");
            RuleFor(c => c.EndOfDayRate).InclusiveBetween(0m, 1000000000m).WithErrorCode("currency_rate_end_of_day_rate_out_of_range").WithMessage("End of day rate is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<CurrencyRateDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.Currencies.AnyAsync(x => x.Id == command.FromCurrencyId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("currency_rate_from_currency_not_found", "From currency not found.");
            }

            if (!await db.Currencies.AnyAsync(x => x.Id == command.ToCurrencyId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("currency_rate_to_currency_not_found", "To currency not found.");
            }

            if (await db.CurrencyRates.AnyAsync(x => x.RateDate == command.RateDate && x.FromCurrencyId == command.FromCurrencyId && x.ToCurrencyId == command.ToCurrencyId, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_rate_conflict", "A exchange rate with the same values already exists.");
            }

            var entity = new CurrencyRate
            {
                Id = Guid.CreateVersion7(),
                RateDate = command.RateDate,
                FromCurrencyId = command.FromCurrencyId,
                ToCurrencyId = command.ToCurrencyId,
                AverageRate = command.AverageRate,
                EndOfDayRate = command.EndOfDayRate,
            };

            db.CurrencyRates.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.CurrencyRates.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(CurrencyRateDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}