using Dotnetstore.PersonalApp.Modules.Schedule.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Persistence;

internal sealed class ScheduleEntryConfiguration : IEntityTypeConfiguration<ScheduleEntry>
{
    public void Configure(EntityTypeBuilder<ScheduleEntry> builder)
    {
        builder.ToTable("ScheduleEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Day).IsRequired();
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.EndTime).IsRequired();
        builder.Property(e => e.Room).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.Day });
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.SubjectId);
    }
}
