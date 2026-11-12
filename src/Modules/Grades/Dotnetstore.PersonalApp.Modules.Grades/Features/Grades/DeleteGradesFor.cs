using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>Cleans up after another module deleted a teacher, a student or a subject that grades, tests and test results refer to.</summary>
internal static class DeleteGradesFor
{
    public sealed record Command(Guid? UserId = null, Guid? StudentId = null, Guid? SubjectId = null);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public Task HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            await db.Grades
                .Where(g => (command.UserId != null && g.UserId == command.UserId)
                    || (command.StudentId != null && g.StudentId == command.StudentId)
                    || (command.SubjectId != null && g.SubjectId == command.SubjectId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // A teacher's tests take their results with them (cascade); a student's results go on their own.
            await db.Tests
                .Where(t => command.UserId != null && t.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await db.TestResults
                .Where(r => command.StudentId != null && r.StudentId == command.StudentId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
