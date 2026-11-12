using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class DeleteTestResult
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.TestResults
                .Where(r => r.Id == command.Id && r.Test.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("test_result_not_found", "Result not found.") : command.Id;
        }
    }
}
