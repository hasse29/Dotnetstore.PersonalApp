using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="ProductsModule"/>.
/// </summary>
internal sealed class ProductsDbContextFactory : IDesignTimeDbContextFactory<ProductsDbContext>
{
    public ProductsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ProductsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}