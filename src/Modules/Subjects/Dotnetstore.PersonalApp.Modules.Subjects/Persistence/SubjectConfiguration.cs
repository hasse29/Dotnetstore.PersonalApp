using Dotnetstore.PersonalApp.Modules.Subjects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Persistence;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(s => s.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case: "math" clashes with "Math".
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
