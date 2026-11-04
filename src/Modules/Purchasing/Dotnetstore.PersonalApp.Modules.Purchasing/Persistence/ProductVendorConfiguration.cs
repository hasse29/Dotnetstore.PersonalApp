using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

internal sealed class ProductVendorConfiguration : IEntityTypeConfiguration<ProductVendor>
{
    public void Configure(EntityTypeBuilder<ProductVendor> builder)
    {
        builder.ToTable("ProductVendors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.AverageLeadTime).IsRequired();
        builder.Property(x => x.StandardPrice).IsRequired();
        builder.Property(x => x.LastReceiptCost);
        builder.Property(x => x.LastReceiptDate);
        builder.Property(x => x.MinOrderQty).IsRequired();
        builder.Property(x => x.MaxOrderQty).IsRequired();
        builder.Property(x => x.OnOrderQty);
        builder.Property(x => x.UnitId).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.VendorId }).IsUnique();
    }
}