using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

internal sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Date).IsRequired();
        builder.Property(g => g.Comment).HasMaxLength(500);
        builder.Property(g => g.CreatedAt).IsRequired();

        // A grade in use keeps its scale step alive; the scale editor reports it instead of silently deleting grades.
        builder.HasOne(g => g.ScaleItem).WithMany().HasForeignKey(g => g.ScaleItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.UserId, g.StudentId });
        builder.HasIndex(g => g.StudentId);
        builder.HasIndex(g => g.SubjectId);
        builder.HasIndex(g => g.ScaleItemId);
    }
}
