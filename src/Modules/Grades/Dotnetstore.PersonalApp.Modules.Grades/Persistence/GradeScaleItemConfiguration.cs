using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

internal sealed class GradeScaleItemConfiguration : IEntityTypeConfiguration<GradeScaleItem>
{
    public void Configure(EntityTypeBuilder<GradeScaleItem> builder)
    {
        builder.ToTable("GradeScaleItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Label).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(200);
        builder.HasIndex(i => i.ScaleId);
    }
}
