using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="AddressesModule"/>.
/// </summary>
internal sealed class AddressesDbContextFactory : IDesignTimeDbContextFactory<AddressesDbContext>
{
    public AddressesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AddressesDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
