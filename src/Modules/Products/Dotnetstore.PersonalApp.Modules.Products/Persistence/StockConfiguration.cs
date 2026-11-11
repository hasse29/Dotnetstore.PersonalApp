using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Products.Persistence;

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.HasIndex(l => new { l.ProductId, l.LocationId }).IsUnique();

        // A product's levels go with it (a product with history cannot be deleted at all); a location with stock cannot be deleted.
        builder.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StorageLocation>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Quantity).HasPrecision(12, 2);
        builder.Property(m => m.Note).HasMaxLength(500);
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.HasIndex(m => m.ProductId);
        builder.HasIndex(m => m.LocationId);

        // History is kept: neither a product nor a location with movements can be deleted.
        builder.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StorageLocation>().WithMany().HasForeignKey(m => m.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(m => m.SupplierId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable("InventoryCounts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(500);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne<StorageLocation>().WithMany().HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.CountId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InventoryCountLineConfiguration : IEntityTypeConfiguration<InventoryCountLine>
{
    public void Configure(EntityTypeBuilder<InventoryCountLine> builder)
    {
        builder.ToTable("InventoryCountLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ExpectedQuantity).HasPrecision(12, 2);
        builder.Property(l => l.CountedQuantity).HasPrecision(12, 2);
        builder.HasIndex(l => new { l.CountId, l.ProductId, l.LocationId }).IsUnique();

        builder.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StorageLocation>().WithMany().HasForeignKey(l => l.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
