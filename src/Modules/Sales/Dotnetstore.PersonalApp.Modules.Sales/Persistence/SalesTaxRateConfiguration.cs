using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesTaxRateConfiguration : IEntityTypeConfiguration<SalesTaxRate>
{
    public void Configure(EntityTypeBuilder<SalesTaxRate> builder)
    {
        builder.ToTable("SalesTaxRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.StateProvince).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TaxType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TaxRate).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}