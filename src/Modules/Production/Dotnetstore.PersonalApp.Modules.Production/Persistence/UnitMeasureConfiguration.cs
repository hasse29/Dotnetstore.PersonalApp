using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class UnitMeasureConfiguration : IEntityTypeConfiguration<UnitMeasure>
{
    public void Configure(EntityTypeBuilder<UnitMeasure> builder)
    {
        builder.ToTable("UnitMeasures");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(10).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new UnitMeasure { Id = new Guid("c427806f-49e5-00e2-3f68-246f245f95d8"), Code = "EA", Name = "Each" },
            new UnitMeasure { Id = new Guid("45a20254-bf77-cd80-1242-3e9bd5b52be8"), Code = "KG", Name = "Kilogram" },
            new UnitMeasure { Id = new Guid("bc9464ee-0014-8262-8b17-441da4501275"), Code = "G", Name = "Gram" },
            new UnitMeasure { Id = new Guid("6d49e26d-53bb-ec2b-753a-60351414f54b"), Code = "M", Name = "Meter" },
            new UnitMeasure { Id = new Guid("220fff24-c7ac-1f8e-a8f4-778bc719fc2f"), Code = "CM", Name = "Centimeter" },
            new UnitMeasure { Id = new Guid("5904109a-6c4b-19bf-8b3a-03fcc443edcf"), Code = "L", Name = "Liter" },
            new UnitMeasure { Id = new Guid("f82bd140-eb35-9024-0a4d-b28f65f26491"), Code = "HR", Name = "Hour" },
            new UnitMeasure { Id = new Guid("6154457f-6237-dcea-fc73-82b5889974a1"), Code = "PK", Name = "Package" });
    }
}