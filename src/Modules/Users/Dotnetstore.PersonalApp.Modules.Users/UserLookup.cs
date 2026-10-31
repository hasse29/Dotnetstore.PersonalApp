using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users;

internal sealed class UserLookup(IDbContextFactory<UsersDbContext> dbFactory) : IUserLookup
{
    public async Task<UserReference?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserReference(u.Id, u.FirstName, u.LastName, u.Username, u.DateOfBirth))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
