using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

internal sealed class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
{
    public void Configure(EntityTypeBuilder<TestResult> builder)
    {
        builder.ToTable("TestResults");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Score).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        // A grade in use keeps its scale step alive, the same as for Grades; the scale editor reports it.
        builder.HasOne(r => r.ScaleItem).WithMany().HasForeignKey(r => r.ScaleItemId).OnDelete(DeleteBehavior.Restrict);

        // A student has one result per test.
        builder.HasIndex(r => new { r.TestId, r.StudentId }).IsUnique();
        builder.HasIndex(r => r.StudentId);
        builder.HasIndex(r => r.ScaleItemId);
    }
}
