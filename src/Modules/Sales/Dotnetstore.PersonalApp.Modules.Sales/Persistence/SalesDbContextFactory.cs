using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="SalesModule"/>.
/// </summary>
internal sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite("Data Source=design-time.db").Options);
}