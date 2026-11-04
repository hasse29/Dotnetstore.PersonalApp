using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesPersonConfiguration : IEntityTypeConfiguration<SalesPerson>
{
    public void Configure(EntityTypeBuilder<SalesPerson> builder)
    {
        builder.ToTable("SalesPeople");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.UserId);
        builder.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.SalesQuota);
        builder.Property(x => x.Bonus).IsRequired();
        builder.Property(x => x.CommissionPct).IsRequired();
        builder.Property(x => x.SalesYtd).IsRequired();
        builder.Property(x => x.SalesLastYear).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}