using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductListPriceHistoryConfiguration : IEntityTypeConfiguration<ProductListPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductListPriceHistory> builder)
    {
        builder.ToTable("ProductListPriceHistories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.Property(x => x.ListPrice).IsRequired();
    }
}