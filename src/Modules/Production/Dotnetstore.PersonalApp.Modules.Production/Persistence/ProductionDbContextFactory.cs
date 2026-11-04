using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="ProductionModule"/>.
/// </summary>
internal sealed class ProductionDbContextFactory : IDesignTimeDbContextFactory<ProductionDbContext>
{
    public ProductionDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ProductionDbContext>().UseSqlite("Data Source=design-time.db").Options);
}