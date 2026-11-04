using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesOrderReasonConfiguration : IEntityTypeConfiguration<SalesOrderReason>
{
    public void Configure(EntityTypeBuilder<SalesOrderReason> builder)
    {
        builder.ToTable("SalesOrderReasons");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesReason).WithMany().HasForeignKey(x => x.SalesReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SalesOrderId, x.SalesReasonId }).IsUnique();
    }
}