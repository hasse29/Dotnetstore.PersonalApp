using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;

internal sealed class PhoneNumberConfiguration : IEntityTypeConfiguration<PhoneNumber>
{
    public void Configure(EntityTypeBuilder<PhoneNumber> builder)
    {
        builder.ToTable("PhoneNumbers");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ObjectId).IsRequired();
        builder.Property(p => p.Number).HasMaxLength(30).IsRequired();
        builder.Property(p => p.NormalizedNumber).HasMaxLength(30).IsRequired();

        // Restrict: a type that is still in use cannot be deleted.
        builder.HasOne(p => p.PhoneNumberType)
            .WithMany()
            .HasForeignKey(p => p.PhoneNumberTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ObjectId, p.NormalizedNumber }).IsUnique();
        builder.HasIndex(p => p.PhoneNumberTypeId);
    }
}