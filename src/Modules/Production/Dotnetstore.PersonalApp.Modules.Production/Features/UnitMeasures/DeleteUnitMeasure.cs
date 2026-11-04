using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal static class DeleteUnitMeasure
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Products.AnyAsync(x => x.SizeUnitId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("unit_measure_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.Products.AnyAsync(x => x.WeightUnitId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("unit_measure_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.BillOfMaterials.AnyAsync(x => x.UnitId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("unit_measure_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.UnitMeasures
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("unit_measure_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}