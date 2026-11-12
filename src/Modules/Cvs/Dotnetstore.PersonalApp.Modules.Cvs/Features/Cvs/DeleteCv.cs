using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal static class DeleteCv
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Cvs
                .Where(c => c.Id == command.Id && c.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("cv_not_found", "CV not found.") : command.Id;
        }
    }
}
