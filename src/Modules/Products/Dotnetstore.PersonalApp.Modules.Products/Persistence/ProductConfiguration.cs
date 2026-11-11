using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        // NOCASE makes the unique index (and number comparisons) ignore case: "cab-1" clashes with "CAB-1".
        builder.Property(p => p.Number).HasMaxLength(30).IsRequired().UseCollation("NOCASE");
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Unit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Photo);
        builder.Property(p => p.PhotoThumbnail);
        builder.Property(p => p.UnitPrice).HasPrecision(12, 2);
        builder.Property(p => p.CostPrice).HasPrecision(12, 2);
        builder.Property(p => p.VatRate).HasPrecision(5, 2);
        builder.Property(p => p.Manufacturer).HasMaxLength(100);
        builder.Property(p => p.Model).HasMaxLength(100);
        builder.Property(p => p.SoftwareVersion).HasMaxLength(50);
        builder.Property(p => p.MinimumHours).HasPrecision(7, 2);
        builder.Property(p => p.MinimumStock).HasPrecision(12, 2);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.Number).IsUnique();
        builder.HasIndex(p => p.Kind);

        // A category in use cannot be deleted; the handler reports that, the restrict is the safety net.
        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
