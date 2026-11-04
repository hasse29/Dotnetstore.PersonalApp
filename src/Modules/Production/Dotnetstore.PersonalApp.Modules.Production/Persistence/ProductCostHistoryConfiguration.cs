using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductCostHistoryConfiguration : IEntityTypeConfiguration<ProductCostHistory>
{
    public void Configure(EntityTypeBuilder<ProductCostHistory> builder)
    {
        builder.ToTable("ProductCostHistories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.Property(x => x.StandardCost).IsRequired();
    }
}