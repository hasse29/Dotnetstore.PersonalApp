using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal static class GetCv
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public Task<Result<CvDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<CvDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var cv = await db.Cvs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == query.UserId, cancellationToken)
                .ConfigureAwait(false);

            return cv is null ? Error.NotFound("cv_not_found", "CV not found.") : CvDto.From(cv);
        }
    }
}
