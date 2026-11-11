using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(t => t.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case: "cables" clashes with "Cables".
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
