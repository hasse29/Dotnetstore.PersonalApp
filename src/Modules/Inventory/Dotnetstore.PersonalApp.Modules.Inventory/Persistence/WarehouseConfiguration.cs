using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Persistence;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Address).HasMaxLength(200);
        builder.Property(x => x.Active);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new Warehouse { Id = new Guid("28b9a5fd-f3e4-ff2d-b9d7-bea26a8742fc"), Code = "MAIN", Name = "Main warehouse", Address = null, Active = true });
    }
}