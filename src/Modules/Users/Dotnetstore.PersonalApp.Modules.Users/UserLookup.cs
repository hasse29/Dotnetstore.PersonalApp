using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users;

internal sealed class UserLookup(IDbContextFactory<UsersDbContext> dbFactory) : IUserLookup, IUserPhotoLookup
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

    public async Task<IReadOnlyList<UserReference>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Users.AsNoTracking()
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UserReference(u.Id, u.FirstName, u.LastName, u.Username, u.DateOfBirth))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<byte[]?> GetThumbnailAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PhotoThumbnail)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
