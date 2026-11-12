using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal static class CreateOffer
{
    public sealed record Command(OfferFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields).SetValidator(new OfferFieldsValidator());
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock, IEnumerable<IContactInfoSource> contactSources)
    {
        public Task<Result<OfferDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<OfferDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var fields = command.Fields;

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == fields.CustomerId, cancellationToken).ConfigureAwait(false);
            if (customer is null)
            {
                return Error.NotFound("offer_customer_not_found", "Customer not found.");
            }

            if (!customer.IsActive)
            {
                return Error.Validation("offer_customer_inactive", "The customer is inactive.");
            }

            var offer = new Offer
            {
                Id = Guid.CreateVersion7(),
                Number = await NextNumberAsync(db, DateOnly.FromDateTime(clock.GetLocalNow().DateTime), cancellationToken).ConfigureAwait(false),
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerAddress = await CustomerAddressBlock.ResolveAsync(contactSources, customer.Id, cancellationToken).ConfigureAwait(false),
                Status = OfferStatus.Draft,
                CreatedAt = clock.GetUtcNow(),
            };
            OfferRules.Apply(offer, fields);
            offer.Lines.AddRange(fields.Lines.Select((l, i) => OfferRules.ToLine(l, i + 1)));

            db.Offers.Add(offer);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return OfferDetailDto.From(offer, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        }

        // The counter restarts every day: one more than the highest used so far, so a deleted draft never makes a number repeat.
        private static async Task<string> NextNumberAsync(SalesDbContext db, DateOnly date, CancellationToken cancellationToken)
        {
            var prefix = DocumentNumber.Prefix(DocumentNumber.Offer, date);
            var used = await db.Offers
                .Where(o => o.Number.StartsWith(prefix))
                .Select(o => o.Number)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return DocumentNumber.Next(prefix, used);
        }
    }
}
