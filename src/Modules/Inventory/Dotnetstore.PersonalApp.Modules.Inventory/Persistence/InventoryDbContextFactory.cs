using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="InventoryModule"/>.
/// </summary>
internal sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite("Data Source=design-time.db").Options);
}