using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;

internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNumber).HasMaxLength(15).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.CreditRating).IsRequired();
        builder.Property(x => x.PreferredVendorStatus);
        builder.Property(x => x.ActiveFlag);
        builder.Property(x => x.PurchasingWebServiceUrl).HasMaxLength(1024);
        builder.Property(x => x.ClientId);
        builder.HasIndex(x => x.AccountNumber).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}