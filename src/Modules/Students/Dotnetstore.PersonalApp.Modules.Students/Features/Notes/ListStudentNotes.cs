using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

internal static class ListStudentNotes
{
    public sealed record Query(Guid StudentId);

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory)
    {
        /// <summary>The latest entry first: by the date the teacher gave the note, then by when it was written.</summary>
        public Task<IReadOnlyList<StudentNoteDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<IReadOnlyList<StudentNoteDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var notes = await db.StudentNotes.AsNoTracking()
                .Where(n => n.StudentId == query.StudentId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Ordered in memory: SQLite cannot order DateTimeOffset values server-side.
            return [.. notes.OrderByDescending(n => n.Date).ThenByDescending(n => n.CreatedAt).Select(StudentNoteDto.From)];
        }
    }
}
