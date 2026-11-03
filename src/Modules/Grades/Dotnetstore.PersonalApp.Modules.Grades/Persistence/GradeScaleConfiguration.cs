using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

internal sealed class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
{
    public void Configure(EntityTypeBuilder<GradeScale> builder)
    {
        builder.ToTable("GradeScales");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.ScaleId).OnDelete(DeleteBehavior.Cascade);
    }
}
