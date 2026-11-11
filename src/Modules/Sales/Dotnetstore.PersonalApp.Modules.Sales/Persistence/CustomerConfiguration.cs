using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case.
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
        builder.Property(c => c.OrganizationNumber).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Street).HasMaxLength(200);
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
