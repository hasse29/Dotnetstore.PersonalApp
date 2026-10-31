using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;

internal sealed class PhoneNumbersDbContext(DbContextOptions<PhoneNumbersDbContext> options) : DbContext(options)
{
    public DbSet<PhoneNumberType> PhoneNumberTypes => Set<PhoneNumberType>();

    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PhoneNumbersDbContext).Assembly);
    }
}
