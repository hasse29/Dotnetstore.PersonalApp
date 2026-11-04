using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SpecialOfferConfiguration : IEntityTypeConfiguration<SpecialOffer>
{
    public void Configure(EntityTypeBuilder<SpecialOffer> builder)
    {
        builder.ToTable("SpecialOffers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(255).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.DiscountPct).IsRequired();
        builder.Property(x => x.OfferType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.MinQty).IsRequired();
        builder.Property(x => x.MaxQty);
        builder.HasIndex(x => x.Description).IsUnique();
        builder.HasData(
            new SpecialOffer { Id = new Guid("55d06aaf-8cfb-eb50-cfa0-ed338f3b879a"), Description = "No discount", DiscountPct = 0m, OfferType = "No discount", Category = "No discount", StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2099, 12, 31), MinQty = 0 });
    }
}