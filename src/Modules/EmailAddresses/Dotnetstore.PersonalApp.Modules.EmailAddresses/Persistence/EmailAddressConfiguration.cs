using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;

internal sealed class EmailAddressConfiguration : IEntityTypeConfiguration<EmailAddress>
{
    public void Configure(EntityTypeBuilder<EmailAddress> builder)
    {
        builder.ToTable("EmailAddresses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ObjectId).IsRequired();

        // NOCASE: "Ada@Example.com" and "ada@example.com" are the same address for an object.
        builder.Property(e => e.Email).HasMaxLength(254).IsRequired().UseCollation("NOCASE");

        // Restrict: a type that is still in use cannot be deleted.
        builder.HasOne(e => e.EmailAddressType)
            .WithMany()
            .HasForeignKey(e => e.EmailAddressTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ObjectId, e.Email }).IsUnique();
        builder.HasIndex(e => e.EmailAddressTypeId);
    }
}