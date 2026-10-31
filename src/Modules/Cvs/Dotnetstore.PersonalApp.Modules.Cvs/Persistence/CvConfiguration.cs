using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Persistence;

internal sealed class CvConfiguration : IEntityTypeConfiguration<Cv>
{
    public void Configure(EntityTypeBuilder<Cv> builder)
    {
        builder.ToTable("Cvs");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.UserId).IsRequired();

        // NOCASE makes the unique index (and title comparisons) ignore case: "backend" clashes with "Backend".
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
        builder.Property(c => c.TargetJob).HasMaxLength(200);
        builder.Property(c => c.Summary).HasMaxLength(4000);
        builder.Property(c => c.LinkedIn).HasMaxLength(300);
        builder.Property(c => c.DriverLicence).HasMaxLength(100).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.Title }).IsUnique();
    }
}
