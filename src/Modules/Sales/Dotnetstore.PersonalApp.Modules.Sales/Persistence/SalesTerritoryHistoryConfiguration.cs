using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesTerritoryHistoryConfiguration : IEntityTypeConfiguration<SalesTerritoryHistory>
{
    public void Configure(EntityTypeBuilder<SalesTerritoryHistory> builder)
    {
        builder.ToTable("SalesTerritoryHistories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
    }
}