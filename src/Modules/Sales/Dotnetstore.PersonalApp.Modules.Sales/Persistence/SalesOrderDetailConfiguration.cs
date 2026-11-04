using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesOrderDetailConfiguration : IEntityTypeConfiguration<SalesOrderDetail>
{
    public void Configure(EntityTypeBuilder<SalesOrderDetail> builder)
    {
        builder.ToTable("SalesOrderDetails");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.CarrierTrackingNumber).HasMaxLength(25);
        builder.Property(x => x.OrderQty).IsRequired();
        builder.HasOne(x => x.SpecialOffer).WithMany().HasForeignKey(x => x.SpecialOfferId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.UnitPrice).IsRequired();
        builder.Property(x => x.UnitPriceDiscount).IsRequired();
        builder.Property(x => x.LineTotal).IsRequired();
    }
}