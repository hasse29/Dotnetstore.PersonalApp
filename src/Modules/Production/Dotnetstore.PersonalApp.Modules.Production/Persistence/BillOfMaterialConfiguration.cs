using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class BillOfMaterialConfiguration : IEntityTypeConfiguration<BillOfMaterial>
{
    public void Configure(EntityTypeBuilder<BillOfMaterial> builder)
    {
        builder.ToTable("BillOfMaterials");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ProductAssembly).WithMany().HasForeignKey(x => x.ProductAssemblyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.BomLevel).IsRequired();
        builder.Property(x => x.PerAssemblyQty).IsRequired();
    }
}