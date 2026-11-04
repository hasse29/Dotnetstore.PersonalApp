using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).HasMaxLength(25).IsRequired().UseCollation("NOCASE");
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.OrderQty).IsRequired();
        builder.Property(x => x.ScrappedQty).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.Property(x => x.DueDate).IsRequired();
        builder.HasOne(x => x.ScrapReason).WithMany().HasForeignKey(x => x.ScrapReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Number).IsUnique();
    }
}