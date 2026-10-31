using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

/// <summary>Lists the addresses of one object (a user, client, ...). There is deliberately no "list everything" query.</summary>
internal static class ListAddresses
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid ObjectId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<AddressDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<AddressesDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var addresses = db.Addresses.AsNoTracking().Where(a => a.ObjectId == query.ObjectId);

            var total = await addresses.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await addresses
                .OrderBy(a => a.AddressType.Name)
                .ThenBy(a => a.City)
                .ThenBy(a => a.Street1)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AddressDto(
                    a.Id,
                    a.ObjectId,
                    a.AddressTypeId,
                    a.AddressType.Name,
                    a.CareOf,
                    a.Street1,
                    a.Street2,
                    a.PostalCode,
                    a.City,
                    a.State,
                    a.Country))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
