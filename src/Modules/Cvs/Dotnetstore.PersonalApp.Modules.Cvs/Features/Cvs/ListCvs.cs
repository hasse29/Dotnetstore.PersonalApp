using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Lists one user's CVs by title. There is deliberately no "list everyone's CVs" query.</summary>
internal static class ListCvs
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<CvDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var cvs = db.Cvs.AsNoTracking().Where(c => c.UserId == query.UserId);

            var total = await cvs.CountAsync(cancellationToken).ConfigureAwait(false);
            var rows = await cvs
                .OrderBy(c => c.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows.ConvertAll(CvDto.From), total);
        }
    }
}
