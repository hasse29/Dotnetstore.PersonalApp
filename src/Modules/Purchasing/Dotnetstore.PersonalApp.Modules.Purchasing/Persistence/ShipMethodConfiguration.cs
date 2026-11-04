using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

internal sealed class ShipMethodConfiguration : IEntityTypeConfiguration<ShipMethod>
{
    public void Configure(EntityTypeBuilder<ShipMethod> builder)
    {
        builder.ToTable("ShipMethods");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.ShipBase).IsRequired();
        builder.Property(x => x.ShipRate).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new ShipMethod { Id = new Guid("b789b452-1410-2d31-347f-d5704c573e86"), Name = "Standard", ShipBase = 3.95m, ShipRate = 0.99m },
            new ShipMethod { Id = new Guid("d1547001-fd74-cce0-9c7c-3fb456bb945f"), Name = "Express", ShipBase = 9.95m, ShipRate = 1.99m },
            new ShipMethod { Id = new Guid("949cd9ad-9003-67be-5adf-d4e688d54c28"), Name = "Overnight", ShipBase = 21.95m, ShipRate = 2.99m });
    }
}