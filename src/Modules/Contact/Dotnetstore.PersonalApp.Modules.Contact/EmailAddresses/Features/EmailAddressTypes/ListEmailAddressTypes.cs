using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;

internal static class ListEmailAddressTypes
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<EmailAddressTypeDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ContactDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var types = db.EmailAddressTypes.AsNoTracking();
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
                .Select(t => new EmailAddressTypeDto(t.Id, t.Name, t.Description, t.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
