using Dotnetstore.PersonalApp.Modules.Subjects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Persistence;

internal sealed class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> builder)
    {
        builder.ToTable("TeacherSubjects");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CreatedAt).IsRequired();

        // Deleting a subject takes it off every teacher.
        builder.HasOne<Subject>().WithMany().HasForeignKey(t => t.SubjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => new { t.UserId, t.SubjectId }).IsUnique();
    }
}
