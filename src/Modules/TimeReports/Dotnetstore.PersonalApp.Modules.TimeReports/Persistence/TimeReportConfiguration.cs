using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

internal sealed class TimeReportConfiguration : IEntityTypeConfiguration<TimeReport>
{
    public void Configure(EntityTypeBuilder<TimeReport> builder)
    {
        builder.ToTable("TimeReports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.ClientId).IsRequired();
        builder.Property(r => r.WorkTimeTypeId).IsRequired();
        builder.Property(r => r.Date).IsRequired();
        builder.Property(r => r.StartTime).IsRequired();
        builder.Property(r => r.EndTime).IsRequired();
        builder.Property(r => r.TotalHours).HasPrecision(5, 2);
        builder.Property(r => r.Location).HasMaxLength(200);
        builder.Property(r => r.DrivingKilometers).HasPrecision(7, 1);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.UserId, r.Date });
        builder.HasIndex(r => r.ClientId);
        builder.HasIndex(r => r.WorkTimeTypeId);
    }
}