using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

internal sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("StorageLocations");
        builder.HasKey(l => l.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case.
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.HasIndex(l => l.Name).IsUnique();
    }
}
