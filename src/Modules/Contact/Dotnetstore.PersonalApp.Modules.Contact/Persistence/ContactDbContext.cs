using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Domain;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Persistence;

internal sealed class ContactDbContext(DbContextOptions<ContactDbContext> options) : DbContext(options)
{
    public DbSet<AddressType> AddressTypes => Set<AddressType>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<EmailAddressType> EmailAddressTypes => Set<EmailAddressType>();

    public DbSet<EmailAddress> EmailAddresses => Set<EmailAddress>();

    public DbSet<PhoneNumberType> PhoneNumberTypes => Set<PhoneNumberType>();

    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContactDbContext).Assembly);
    }
}
