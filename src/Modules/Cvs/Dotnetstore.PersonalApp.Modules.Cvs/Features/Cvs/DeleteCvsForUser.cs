using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Deletes every CV of one user. Having no CVs is not an error: the result is the number deleted.</summary>
internal static class DeleteCvsForUser
{
    public sealed record Command(Guid UserId);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory)
    {
        public async Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.UserId == Guid.Empty)
            {
                return Error.Validation("cv_user_required", "A CV must belong to a user.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.Cvs
                .Where(c => c.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
