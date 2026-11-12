using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

/// <summary>Reads only the photo, so listing students never drags their pictures along.</summary>
internal static class GetStudentPhoto
{
    public sealed record Query(Guid StudentId);

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory)
    {
        public Task<Result<byte[]?>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<byte[]?>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await db.Students.AsNoTracking()
                .Where(x => x.Id == query.StudentId)
                .Select(x => new { x.Photo })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return found is null ? Error.NotFound("student_not_found", "Student not found.") : Result<byte[]?>.Success(found.Photo);
        }
    }
}
