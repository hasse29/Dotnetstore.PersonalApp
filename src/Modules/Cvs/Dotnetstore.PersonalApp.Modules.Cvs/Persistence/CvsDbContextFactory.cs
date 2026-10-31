using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="CvsModule"/>.
/// </summary>
internal sealed class CvsDbContextFactory : IDesignTimeDbContextFactory<CvsDbContext>
{
    public CvsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CvsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
