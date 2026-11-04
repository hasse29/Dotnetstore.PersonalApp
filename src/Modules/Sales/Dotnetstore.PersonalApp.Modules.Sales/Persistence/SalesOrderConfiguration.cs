using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).HasMaxLength(25).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.OnlineOrderFlag);
        builder.Property(x => x.OrderDate).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.ShipDate);
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(25);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ShipMethodId);
        builder.Property(x => x.TaxAmt).IsRequired();
        builder.Property(x => x.Freight).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(128);
        builder.Property(x => x.SubTotal).IsRequired();
        builder.Property(x => x.TotalDue).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
    }
}