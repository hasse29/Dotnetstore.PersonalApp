using Dotnetstore.PersonalApp.Modules.Addresses.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Persistence;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ObjectId).IsRequired();
        builder.Property(a => a.CareOf).HasMaxLength(100);
        builder.Property(a => a.Street1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Street2).HasMaxLength(200);
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.Country).HasMaxLength(100).IsRequired();

        // Restrict: a type that is still in use cannot be deleted.
        builder.HasOne(a => a.AddressType)
            .WithMany()
            .HasForeignKey(a => a.AddressTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ObjectId);
        builder.HasIndex(a => a.AddressTypeId);
    }
}
