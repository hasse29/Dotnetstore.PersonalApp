using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class WorkOrderRoutingConfiguration : IEntityTypeConfiguration<WorkOrderRouting>
{
    public void Configure(EntityTypeBuilder<WorkOrderRouting> builder)
    {
        builder.ToTable("WorkOrderRoutings");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.OperationSequence).IsRequired();
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ScheduledStartDate).IsRequired();
        builder.Property(x => x.ScheduledEndDate).IsRequired();
        builder.Property(x => x.ActualStartDate);
        builder.Property(x => x.ActualEndDate);
        builder.Property(x => x.ActualResourceHrs);
        builder.Property(x => x.PlannedCost).IsRequired();
        builder.Property(x => x.ActualCost);
        builder.HasIndex(x => new { x.WorkOrderId, x.OperationSequence }).IsUnique();
    }
}