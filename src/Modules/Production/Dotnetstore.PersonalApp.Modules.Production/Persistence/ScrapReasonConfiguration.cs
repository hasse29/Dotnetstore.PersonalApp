using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ScrapReasonConfiguration : IEntityTypeConfiguration<ScrapReason>
{
    public void Configure(EntityTypeBuilder<ScrapReason> builder)
    {
        builder.ToTable("ScrapReasons");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new ScrapReason { Id = new Guid("a111462e-3b4b-417f-caf6-d6d8e66f2ef1"), Name = "Drill size too large" },
            new ScrapReason { Id = new Guid("4380c3dc-ad99-a4e2-30a3-2cf4005b6015"), Name = "Drill size too small" },
            new ScrapReason { Id = new Guid("3faf40d0-3956-936d-bf06-2bd56bb1bed9"), Name = "Gouge in metal" },
            new ScrapReason { Id = new Guid("d5bf1b19-533f-aefd-3bd7-bed272e65259"), Name = "Paint process failed" },
            new ScrapReason { Id = new Guid("b189ff67-9dd6-4764-9fe4-645916b3dcba"), Name = "Wrong size" });
    }
}