using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(3).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new Currency { Id = new Guid("1918ecc6-44b5-84af-8139-697d8473a3e9"), Code = "SEK", Name = "Swedish krona" },
            new Currency { Id = new Guid("4cd461f3-c5cf-9f69-b7c5-5ae24f4eeebf"), Code = "USD", Name = "US dollar" },
            new Currency { Id = new Guid("5a118829-8c41-357a-1f3a-450d7db1a6f5"), Code = "EUR", Name = "Euro" },
            new Currency { Id = new Guid("e9f6ae11-d9c8-0341-05da-7aed24c3cc84"), Code = "GBP", Name = "Pound sterling" },
            new Currency { Id = new Guid("d00b4a10-f7f6-e190-ec4e-96ce04efaf52"), Code = "NOK", Name = "Norwegian krone" },
            new Currency { Id = new Guid("374bdb9e-e8ab-d0ea-41e2-8469a7b8248a"), Code = "DKK", Name = "Danish krone" },
            new Currency { Id = new Guid("6cf2c02d-1039-f9f0-3bca-cb3b98ac2847"), Code = "CNY", Name = "Yuan renminbi" });
    }
}