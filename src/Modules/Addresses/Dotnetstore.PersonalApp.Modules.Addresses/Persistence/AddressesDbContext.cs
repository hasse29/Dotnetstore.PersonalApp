using Dotnetstore.PersonalApp.Modules.Addresses.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Persistence;

internal sealed class AddressesDbContext(DbContextOptions<AddressesDbContext> options) : DbContext(options)
{
    public DbSet<AddressType> AddressTypes => Set<AddressType>();

    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AddressesDbContext).Assembly);
    }
}
