using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Number).HasMaxLength(30).IsRequired().UseCollation("NOCASE");
        builder.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CustomerAddress).HasMaxLength(400);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.Reference).HasMaxLength(200);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => o.Number).IsUnique();
        builder.HasIndex(o => o.CustomerId);

        // A customer with offers cannot be deleted; the offers are the history.
        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OfferId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OfferLineConfiguration : IEntityTypeConfiguration<OfferLine>
{
    public void Configure(EntityTypeBuilder<OfferLine> builder)
    {
        builder.ToTable("OfferLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Unit).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(12, 2);
        builder.Property(l => l.VatRate).HasPrecision(5, 2);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);

        builder.HasIndex(l => new { l.OfferId, l.Position });
    }
}
