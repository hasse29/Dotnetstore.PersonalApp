using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesPersonQuotaHistoryConfiguration : IEntityTypeConfiguration<SalesPersonQuotaHistory>
{
    public void Configure(EntityTypeBuilder<SalesPersonQuotaHistory> builder)
    {
        builder.ToTable("SalesPersonQuotaHistories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.QuotaDate).IsRequired();
        builder.Property(x => x.SalesQuota).IsRequired();
        builder.HasIndex(x => new { x.SalesPersonId, x.QuotaDate }).IsUnique();
    }
}