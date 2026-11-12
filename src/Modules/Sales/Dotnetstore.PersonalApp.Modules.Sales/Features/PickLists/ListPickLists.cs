using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

internal static class ListPickLists
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, PickListStatus? Status = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<PickListDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var pickLists = db.PickLists.AsNoTracking();
            if (query.Status is { } status)
            {
                pickLists = pickLists.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                var orderIds = db.Orders.Where(o => EF.Functions.Like(o.Number, pattern) || EF.Functions.Like(o.CustomerName, pattern)).Select(o => o.Id);
                pickLists = pickLists.Where(p => EF.Functions.Like(p.Number, pattern) || orderIds.Contains(p.OrderId));
            }

            var total = await pickLists.CountAsync(cancellationToken).ConfigureAwait(false);

            // The newest first: the id is a version 7 Guid, so it sorts by creation time.
            var rows = await pickLists
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Lines)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var ids = rows.Select(r => r.OrderId).ToList();
            var orders = await db.Orders.AsNoTracking().Where(o => ids.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken).ConfigureAwait(false);

            return new Response([.. rows.Select(p => PickListDetailDto.ToDto(p, orders[p.OrderId]))], total);
        }
    }
}
