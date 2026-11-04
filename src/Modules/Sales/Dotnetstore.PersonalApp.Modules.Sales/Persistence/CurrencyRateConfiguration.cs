using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.ToTable("CurrencyRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RateDate).IsRequired();
        builder.HasOne(x => x.FromCurrency).WithMany().HasForeignKey(x => x.FromCurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToCurrency).WithMany().HasForeignKey(x => x.ToCurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.AverageRate).IsRequired();
        builder.Property(x => x.EndOfDayRate).IsRequired();
        builder.HasIndex(x => new { x.RateDate, x.FromCurrencyId, x.ToCurrencyId }).IsUnique();
    }
}