using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

internal sealed class PurchaseOrderDetailConfiguration : IEntityTypeConfiguration<PurchaseOrderDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderDetail> builder)
    {
        builder.ToTable("PurchaseOrderDetails");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.OrderQty).IsRequired();
        builder.Property(x => x.UnitPrice).IsRequired();
        builder.Property(x => x.ReceivedQty).IsRequired();
        builder.Property(x => x.RejectedQty).IsRequired();
        builder.Property(x => x.LineTotal).IsRequired();
        builder.Property(x => x.StockedQty).IsRequired();
    }
}