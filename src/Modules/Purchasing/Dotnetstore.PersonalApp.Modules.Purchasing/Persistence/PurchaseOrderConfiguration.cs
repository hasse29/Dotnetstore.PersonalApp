using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).HasMaxLength(25).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BuyerId);
        builder.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShipMethod).WithMany().HasForeignKey(x => x.ShipMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.OrderDate).IsRequired();
        builder.Property(x => x.ShipDate);
        builder.Property(x => x.TaxAmt).IsRequired();
        builder.Property(x => x.Freight).IsRequired();
        builder.Property(x => x.SubTotal).IsRequired();
        builder.Property(x => x.TotalDue).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
    }
}