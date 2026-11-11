using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case.
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
        builder.Property(s => s.OrganizationNumber).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Website).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

internal sealed class SupplierPriceConfiguration : IEntityTypeConfiguration<SupplierPrice>
{
    public void Configure(EntityTypeBuilder<SupplierPrice> builder)
    {
        builder.ToTable("SupplierPrices");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SupplierArticleNumber).HasMaxLength(50);
        builder.Property(p => p.Price).HasPrecision(12, 2);
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => new { p.SupplierId, p.ProductId }).IsUnique();
        builder.HasIndex(p => p.ProductId);

        // Prices follow their supplier and their product: neither is meaningful on its own.
        builder.HasOne<Supplier>().WithMany(s => s.Prices).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>().WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
