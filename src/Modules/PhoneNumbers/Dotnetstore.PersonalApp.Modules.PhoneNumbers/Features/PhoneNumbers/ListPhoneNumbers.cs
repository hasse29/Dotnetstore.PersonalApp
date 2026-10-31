using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

/// <summary>Lists the addresses of one object (a user, client, ...). There is deliberately no "list everything" query.</summary>
internal static class ListPhoneNumbers
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid ObjectId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<PhoneNumberDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<PhoneNumbersDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var addresses = db.PhoneNumbers.AsNoTracking().Where(a => a.ObjectId == query.ObjectId);

            var total = await addresses.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await addresses
                .OrderBy(a => a.PhoneNumberType.Name)
                .ThenBy(a => a.Number)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new PhoneNumberDto(
                    a.Id,
                    a.ObjectId,
                    a.PhoneNumberTypeId,
                    a.PhoneNumberType.Name,
                    a.Number))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
