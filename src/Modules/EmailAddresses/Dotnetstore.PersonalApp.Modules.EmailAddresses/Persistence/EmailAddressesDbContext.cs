using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;

internal sealed class EmailAddressesDbContext(DbContextOptions<EmailAddressesDbContext> options) : DbContext(options)
{
    public DbSet<EmailAddressType> EmailAddressTypes => Set<EmailAddressType>();

    public DbSet<EmailAddress> EmailAddresses => Set<EmailAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailAddressesDbContext).Assembly);
    }
}
