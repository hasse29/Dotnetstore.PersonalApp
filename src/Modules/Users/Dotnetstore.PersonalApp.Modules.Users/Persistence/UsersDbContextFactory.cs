using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Users.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="UsersModule"/>.
/// </summary>
internal sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<UsersDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
