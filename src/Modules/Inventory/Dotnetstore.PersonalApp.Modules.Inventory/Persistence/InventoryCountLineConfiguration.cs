using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Persistence;

internal sealed class InventoryCountLineConfiguration : IEntityTypeConfiguration<InventoryCountLine>
{
    public void Configure(EntityTypeBuilder<InventoryCountLine> builder)
    {
        builder.ToTable("InventoryCountLines");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.InventoryCount).WithMany().HasForeignKey(x => x.InventoryCountId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.ProductId).IsRequired();
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ExpectedQty).IsRequired();
        builder.Property(x => x.CountedQty);
        builder.Property(x => x.Difference).IsRequired();
        builder.HasIndex(x => new { x.InventoryCountId, x.ProductId, x.LocationId }).IsUnique();
    }
}