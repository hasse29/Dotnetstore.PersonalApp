using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SpecialOfferProductConfiguration : IEntityTypeConfiguration<SpecialOfferProduct>
{
    public void Configure(EntityTypeBuilder<SpecialOfferProduct> builder)
    {
        builder.ToTable("SpecialOfferProducts");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SpecialOffer).WithMany().HasForeignKey(x => x.SpecialOfferId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.ProductId).IsRequired();
        builder.HasIndex(x => new { x.SpecialOfferId, x.ProductId }).IsUnique();
    }
}