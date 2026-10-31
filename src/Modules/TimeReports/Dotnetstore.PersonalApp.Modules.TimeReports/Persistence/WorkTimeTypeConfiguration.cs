using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

internal sealed class WorkTimeTypeConfiguration : IEntityTypeConfiguration<WorkTimeType>
{
    public void Configure(EntityTypeBuilder<WorkTimeType> builder)
    {
        builder.ToTable("WorkTimeTypes");
        builder.HasKey(t => t.Id);

        // NOCASE makes the unique index (and name comparisons) ignore case: "sick" clashes with "Sick".
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();

        // Seeded once by the migration; afterwards the types belong to the user, who can rename or delete them.
        builder.HasData(WorkTimeTypeSeed.All);
    }
}
