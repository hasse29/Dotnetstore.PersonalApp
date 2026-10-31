using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Persistence;

// Every section row belongs to one CV and is deleted with it (the database cascades, so bulk deletes of CVs stay correct).

internal sealed class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("WorkExperiences");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Employer).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Role).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.Technologies).HasMaxLength(1000).IsRequired().HasDefaultValue(string.Empty);
        builder.HasOne<Cv>().WithMany().HasForeignKey(w => w.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.CvId);
    }
}

internal sealed class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("Educations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.School).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Programme).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.HasOne<Cv>().WithMany().HasForeignKey(e => e.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.CvId);
    }
}

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Provider).HasMaxLength(200);
        builder.HasOne<Cv>().WithMany().HasForeignKey(c => c.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.CvId);
    }
}

internal sealed class LanguageSkillConfiguration : IEntityTypeConfiguration<LanguageSkill>
{
    public void Configure(EntityTypeBuilder<LanguageSkill> builder)
    {
        builder.ToTable("LanguageSkills");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Language).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(l => l.Level).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Cv>().WithMany().HasForeignKey(l => l.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.CvId, l.Language }).IsUnique();
    }
}

internal sealed class CvContactSelectionConfiguration : IEntityTypeConfiguration<CvContactSelection>
{
    public void Configure(EntityTypeBuilder<CvContactSelection> builder)
    {
        builder.ToTable("CvContactSelections");
        builder.HasKey(s => s.Id);
        builder.HasOne<Cv>().WithMany().HasForeignKey(s => s.CvId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.CvId, s.ContactId }).IsUnique();
    }
}
