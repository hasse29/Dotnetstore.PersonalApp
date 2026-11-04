using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal static class DeleteProduct
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IEventPublisher events)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.BillOfMaterials.AnyAsync(x => x.ProductAssemblyId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.BillOfMaterials.AnyAsync(x => x.ComponentId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.WorkOrders.AnyAsync(x => x.ProductId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.TransactionHistories.AnyAsync(x => x.ProductId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.Products
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("product_not_found", "The record was not found.");
            }

            await events.PublishAsync(new ProductDeleted(command.Id), cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}