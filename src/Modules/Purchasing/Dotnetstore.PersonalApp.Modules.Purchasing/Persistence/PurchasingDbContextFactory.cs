using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="PurchasingModule"/>.
/// </summary>
internal sealed class PurchasingDbContextFactory : IDesignTimeDbContextFactory<PurchasingDbContext>
{
    public PurchasingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PurchasingDbContext>().UseSqlite("Data Source=design-time.db").Options);
}