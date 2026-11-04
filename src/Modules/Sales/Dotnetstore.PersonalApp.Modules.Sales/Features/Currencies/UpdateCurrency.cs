using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

internal static class UpdateCurrency
{
    public sealed record Command(Guid Id, string Code, string Name);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Code).NotEmpty().WithErrorCode("currency_code_required").WithMessage("Code is required.").MaximumLength(3).WithErrorCode("currency_code_too_long").WithMessage("Code is too long.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("currency_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("currency_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<CurrencyDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var codeValue = command.Code.Trim();
            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.Currencies.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("currency_not_found", "The record was not found.");
            }



            if (await db.Currencies.AnyAsync(x => x.Code == codeValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_conflict", "A currency with the same values already exists.");
            }

            if (await db.Currencies.AnyAsync(x => x.Name == nameValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("currency_conflict", "A currency with the same values already exists.");
            }

            entity.Code = codeValue;
            entity.Name = nameValue;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.Currencies.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(CurrencyDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}