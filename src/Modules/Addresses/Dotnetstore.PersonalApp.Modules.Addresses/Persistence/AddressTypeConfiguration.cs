using Dotnetstore.PersonalApp.Modules.Addresses.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Persistence;

internal sealed class AddressTypeConfiguration : IEntityTypeConfiguration<AddressType>
{
    public void Configure(EntityTypeBuilder<AddressType> builder)
    {
        builder.ToTable("AddressTypes");
        builder.HasKey(t => t.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case: "billing" clashes with "Billing".
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();

        // Seeded once by the migration; afterwards the types belong to the user, who can rename or delete them.
        builder.HasData(AddressTypeSeed.All);
    }
}
