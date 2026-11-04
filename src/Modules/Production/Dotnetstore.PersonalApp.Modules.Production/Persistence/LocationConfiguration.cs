using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.CostRate).IsRequired();
        builder.Property(x => x.Availability).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new Location { Id = new Guid("e8e47b31-08d5-6dc6-5689-80a56c60c750"), Name = "Tool Crib", CostRate = 0m, Availability = 0m },
            new Location { Id = new Guid("7e19bdc3-5285-acdf-2a98-b9de07ddf418"), Name = "Sheet Metal Racks", CostRate = 0m, Availability = 0m },
            new Location { Id = new Guid("696e197e-1eaa-399e-211e-f36f7a93e7a7"), Name = "Paint Shop", CostRate = 0m, Availability = 0m },
            new Location { Id = new Guid("d4dda2c5-f2e8-f13b-2e99-dfc3124de1a3"), Name = "Final Assembly", CostRate = 12.25m, Availability = 120m });
    }
}