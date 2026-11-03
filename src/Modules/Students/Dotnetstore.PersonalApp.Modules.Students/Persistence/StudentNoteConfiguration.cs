using Dotnetstore.PersonalApp.Modules.Students.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Students.Persistence;

internal sealed class StudentNoteConfiguration : IEntityTypeConfiguration<StudentNote>
{
    public void Configure(EntityTypeBuilder<StudentNote> builder)
    {
        builder.ToTable("StudentNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Text).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.Date).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.HasIndex(n => new { n.StudentId, n.Date });
        builder.HasOne<Student>().WithMany().HasForeignKey(n => n.StudentId).OnDelete(DeleteBehavior.Cascade);
    }
}
