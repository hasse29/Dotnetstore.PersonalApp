using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

internal static class ListAddressTypes
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<AddressTypeDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<AddressesDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var types = db.AddressTypes.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                types = types.Where(t =>
                    EF.Functions.Like(t.Name, pattern) ||
                    (t.Description != null && EF.Functions.Like(t.Description, pattern)));
            }

            var total = await types.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await types
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new AddressTypeDto(t.Id, t.Name, t.Description, t.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
