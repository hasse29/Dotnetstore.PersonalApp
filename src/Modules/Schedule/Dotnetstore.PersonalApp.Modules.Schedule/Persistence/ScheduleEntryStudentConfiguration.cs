using Dotnetstore.PersonalApp.Modules.Schedule.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Persistence;

internal sealed class ScheduleEntryStudentConfiguration : IEntityTypeConfiguration<ScheduleEntryStudent>
{
    public void Configure(EntityTypeBuilder<ScheduleEntryStudent> builder)
    {
        builder.ToTable("ScheduleEntryStudents");
        builder.HasKey(s => new { s.EntryId, s.StudentId });
        builder.HasIndex(s => s.StudentId);
    }
}
