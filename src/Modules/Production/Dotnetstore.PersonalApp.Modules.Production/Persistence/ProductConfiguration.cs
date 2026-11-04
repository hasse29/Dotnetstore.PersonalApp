using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.ProductNumber).HasMaxLength(25).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.MakeFlag);
        builder.Property(x => x.FinishedGoodsFlag);
        builder.Property(x => x.Color).HasMaxLength(15);
        builder.Property(x => x.SafetyStockLevel).IsRequired();
        builder.Property(x => x.ReorderPoint).IsRequired();
        builder.Property(x => x.StandardCost).IsRequired();
        builder.Property(x => x.ListPrice).IsRequired();
        builder.Property(x => x.Size).HasMaxLength(5);
        builder.HasOne(x => x.SizeUnit).WithMany().HasForeignKey(x => x.SizeUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Weight);
        builder.HasOne(x => x.WeightUnit).WithMany().HasForeignKey(x => x.WeightUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.DaysToManufacture).IsRequired();
        builder.Property(x => x.ProductLine).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Class).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Style).HasMaxLength(20).IsRequired();
        builder.HasOne(x => x.Subcategory).WithMany().HasForeignKey(x => x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.SellStartDate).IsRequired();
        builder.Property(x => x.SellEndDate);
        builder.Property(x => x.DiscontinuedDate);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.ProductNumber).IsUnique();
    }
}