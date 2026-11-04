using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class CountryRegionCurrencyConfiguration : IEntityTypeConfiguration<CountryRegionCurrency>
{
    public void Configure(EntityTypeBuilder<CountryRegionCurrency> builder)
    {
        builder.ToTable("CountryRegionCurrencies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CountryRegion).HasMaxLength(100).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CountryRegion, x.CurrencyId }).IsUnique();
    }
}