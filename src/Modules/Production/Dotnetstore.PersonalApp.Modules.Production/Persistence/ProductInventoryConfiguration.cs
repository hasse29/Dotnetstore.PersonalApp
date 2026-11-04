using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductInventoryConfiguration : IEntityTypeConfiguration<ProductInventory>
{
    public void Configure(EntityTypeBuilder<ProductInventory> builder)
    {
        builder.ToTable("ProductInventories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Shelf).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Bin).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();
    }
}