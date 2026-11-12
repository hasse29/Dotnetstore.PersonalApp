using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

/// <summary>Lists the addresses of one object (a user, client, ...). There is deliberately no "list everything" query.</summary>
internal static class ListEmailAddresses
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid ObjectId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<EmailAddressDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var addresses = db.EmailAddresses.AsNoTracking().Where(a => a.ObjectId == query.ObjectId);

            var total = await addresses.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await addresses
                .OrderBy(a => a.EmailAddressType.Name)
                .ThenBy(a => a.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new EmailAddressDto(
                    a.Id,
                    a.ObjectId,
                    a.EmailAddressTypeId,
                    a.EmailAddressType.Name,
                    a.Email))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
