using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesTerritoryConfiguration : IEntityTypeConfiguration<SalesTerritory>
{
    public void Configure(EntityTypeBuilder<SalesTerritory> builder)
    {
        builder.ToTable("SalesTerritories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.CountryRegionCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.RegionGroup).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SalesYtd).IsRequired();
        builder.Property(x => x.SalesLastYear).IsRequired();
        builder.Property(x => x.CostYtd).IsRequired();
        builder.Property(x => x.CostLastYear).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}