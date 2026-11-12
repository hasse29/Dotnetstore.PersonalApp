using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

/// <summary>Saves the header and all lines of a draft offer; the lines are replaced as a whole.</summary>
internal static class UpdateOffer
{
    public sealed record Command(Guid Id, OfferFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields).SetValidator(new OfferFieldsValidator());
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock, IEnumerable<IContactInfoSource> contactSources)
    {
        public async Task<Result<OfferDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var fields = command.Fields;

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offer = await db.Offers.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (offer is null)
            {
                return Error.NotFound("offer_not_found", "Offer not found.");
            }

            if (offer.Status != OfferStatus.Draft)
            {
                return Error.Conflict("offer_not_editable", "Only a draft offer can be changed.");
            }

            // The copied name and address only change when another customer is picked.
            var customerChanged = offer.CustomerId != fields.CustomerId;
            if (customerChanged)
            {
                var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == fields.CustomerId, cancellationToken).ConfigureAwait(false);
                if (customer is null)
                {
                    return Error.NotFound("offer_customer_not_found", "Customer not found.");
                }

                if (!customer.IsActive)
                {
                    return Error.Validation("offer_customer_inactive", "The customer is inactive.");
                }

                offer.CustomerId = customer.Id;
                offer.CustomerName = customer.Name;
            }

            // A draft is not issued yet, so it picks up the customer's current address; sent offers keep the one they were sent with.
            // No address found (it was removed, or no address source is installed) leaves a draft's existing address alone.
            var address = await CustomerAddressBlock.ResolveAsync(contactSources, offer.CustomerId, cancellationToken).ConfigureAwait(false);
            if (address is not null || customerChanged)
            {
                offer.CustomerAddress = address;
            }

            OfferRules.Apply(offer, fields);

            db.OfferLines.RemoveRange(offer.Lines);
            offer.Lines = [.. fields.Lines.Select((l, i) => OfferRules.ToLine(l, i + 1))];
            db.OfferLines.AddRange(offer.Lines);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return OfferDetailDto.From(offer, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        }
    }
}
