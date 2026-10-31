using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;

internal sealed class EmailAddressTypeConfiguration : IEntityTypeConfiguration<EmailAddressType>
{
    public void Configure(EntityTypeBuilder<EmailAddressType> builder)
    {
        builder.ToTable("EmailAddressTypes");
        builder.HasKey(t => t.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case: "billing" clashes with "Billing".
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();

        // Seeded once by the migration; afterwards the types belong to the user, who can rename or delete them.
        builder.HasData(EmailAddressTypeSeed.All);
    }
}
