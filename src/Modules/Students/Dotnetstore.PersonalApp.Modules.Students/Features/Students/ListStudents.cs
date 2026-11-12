using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal static class ListStudents
{
    /// <param name="ClientId">The school to list, or null for the students of every school.</param>
    public sealed record Query(Guid? ClientId);

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory)
    {
        public Task<IReadOnlyList<StudentDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<IReadOnlyList<StudentDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.Students.AsNoTracking()
                .Where(s => query.ClientId == null || s.ClientId == query.ClientId)
                .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
                .Select(s => new StudentDto(s.Id, s.ClientId, s.FirstName, s.LastName, s.ClassName, s.Notes, s.CreatedAt, s.PhotoThumbnail))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
