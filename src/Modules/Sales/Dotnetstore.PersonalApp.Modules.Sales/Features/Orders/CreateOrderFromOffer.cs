using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

/// <summary>
/// Makes an order, and a pick list for its physical lines, from an accepted offer. Everything is copied, so the order stays as
/// agreed even if the offer's customer changes. An order with nothing to pick is ready to invoice at once.
/// </summary>
internal static class CreateOrderFromOffer
{
    public sealed record Command(Guid OfferId);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<OrderDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<OrderDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offer = await db.Offers.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken).ConfigureAwait(false);
            if (offer is null)
            {
                return Error.NotFound("offer_not_found", "Offer not found.");
            }

            if (offer.Status != OfferStatus.Accepted)
            {
                return Error.Conflict("order_offer_not_accepted", "Only an accepted offer can be made into an order.");
            }

            if (await db.Orders.AnyAsync(o => o.OfferId == offer.Id && o.Status != OrderStatus.Cancelled, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("order_already_exists", "The offer already has an order.");
            }

            var now = clock.GetUtcNow();
            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

            var order = new Order
            {
                Id = Guid.CreateVersion7(),
                Number = await NextNumberAsync(db, today, cancellationToken).ConfigureAwait(false),
                OfferId = offer.Id,
                OfferNumber = offer.Number,
                CustomerId = offer.CustomerId,
                CustomerName = offer.CustomerName,
                CustomerAddress = offer.CustomerAddress,
                Reference = offer.Reference,
                Notes = offer.Notes,
                Date = today,
                Status = OrderStatus.Open,
                CreatedAt = now,
                Lines = [.. offer.Lines.OrderBy(l => l.Position).Select(l => new OrderLine
                {
                    Id = Guid.CreateVersion7(),
                    Position = l.Position,
                    ProductId = l.ProductId,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VatRate = l.VatRate,
                    DiscountPercent = l.DiscountPercent,
                    IsPhysical = l.IsPhysical,
                })],
            };

            var physical = order.Lines.Where(l => l.IsPhysical).ToList();
            if (physical.Count == 0)
            {
                order.Status = OrderStatus.ReadyToInvoice;
            }
            else
            {
                var pickList = new PickList
                {
                    Id = Guid.CreateVersion7(),
                    Number = DocumentNumber.PickList(order.Number),
                    Status = PickListStatus.Open,
                    CreatedAt = now,
                    Lines = [.. physical.Select((l, i) => new PickListLine
                    {
                        Id = Guid.CreateVersion7(),
                        Position = i + 1,
                        ProductId = l.ProductId,
                        Description = l.Description,
                        Unit = l.Unit,
                        Quantity = l.Quantity,
                    })],
                };
                order.PickList = pickList;
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return OrderDetailDto.From(order);
        }
        // The counter restarts every day and is never reused.
        private static async Task<string> NextNumberAsync(SalesDbContext db, DateOnly date, CancellationToken cancellationToken)
        {
            var prefix = DocumentNumber.Prefix(DocumentNumber.Order, date);
            var used = await db.Orders
                .Where(o => o.Number.StartsWith(prefix))
                .Select(o => o.Number)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return DocumentNumber.Next(prefix, used);
        }
    }
}
