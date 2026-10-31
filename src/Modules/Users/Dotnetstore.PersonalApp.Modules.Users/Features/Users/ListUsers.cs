using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal static class ListUsers
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<UserDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var users = db.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                users = users.Where(u =>
                    EF.Functions.Like(u.FirstName, pattern) ||
                    EF.Functions.Like(u.LastName, pattern) ||
                    EF.Functions.Like(u.Username, pattern));
            }

            var total = await users.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await users
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto(u.Id, u.FirstName, u.LastName, u.Username, u.DateOfBirth, u.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
