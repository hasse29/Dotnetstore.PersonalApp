using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence;

internal sealed class ReceivedMessageConfiguration : IEntityTypeConfiguration<ReceivedMessage>
{
    public void Configure(EntityTypeBuilder<ReceivedMessage> builder)
    {
        builder.ToTable("ReceivedMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Uid).HasMaxLength(200).IsRequired();
        builder.Property(m => m.From).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(1000).IsRequired();
        // SQLite cannot order DateTimeOffset text values, so they are stored as sortable numbers.
        builder.Property(m => m.SentAt).HasConversion(new DateTimeOffsetToBinaryConverter()).IsRequired();
        builder.Property(m => m.Body).IsRequired();
        builder.Property(m => m.ToAddresses).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(m => m.CcAddresses).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(m => m.HtmlBody);
        builder.Property(m => m.FetchedAt).HasConversion(new DateTimeOffsetToBinaryConverter()).IsRequired();

        builder.HasOne<EmailAccount>().WithMany().HasForeignKey(m => m.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.AccountId, m.Uid }).IsUnique();
        builder.HasIndex(m => new { m.AccountId, m.SentAt });
    }
}
