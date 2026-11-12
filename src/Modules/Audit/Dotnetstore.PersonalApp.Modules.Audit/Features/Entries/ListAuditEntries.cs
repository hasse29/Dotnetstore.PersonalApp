using Dotnetstore.PersonalApp.Modules.Audit.Persistence;
using Dotnetstore.PersonalApp.Shared.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;

internal static class ListAuditEntries
{
    public const int MaxPageSize = 200;

    public sealed record Query(string? Search = null, bool? Writes = null, bool OnlyFailed = false, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<AuditEntryDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<AuditDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var records = db.Records.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                records = records.Where(r =>
                    EF.Functions.Like(r.Operation, pattern) ||
                    (r.Username != null && EF.Functions.Like(r.Username, pattern)) ||
                    (r.Fields != null && EF.Functions.Like(r.Fields, pattern)) ||
                    (r.ErrorCode != null && EF.Functions.Like(r.ErrorCode, pattern)));
            }

            if (query.Writes is { } writes)
            {
                records = records.Where(r => r.IsWrite == writes);
            }

            if (query.OnlyFailed)
            {
                records = records.Where(r => !r.Succeeded);
            }

            var total = await records.CountAsync(cancellationToken).ConfigureAwait(false);

            // The ids are time ordered (version 7), which sorts reliably in SQLite where DateTimeOffset does not.
            var items = await records
                .OrderByDescending(r => r.Id)
                .Take(pageSize)
                .Select(r => new AuditEntryDto(r.Id, r.Timestamp, r.Username ?? string.Empty, r.Operation, r.IsWrite, r.EntityId, r.Fields, r.Succeeded, r.ErrorCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
