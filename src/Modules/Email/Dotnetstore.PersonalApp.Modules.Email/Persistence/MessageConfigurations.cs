using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence;

internal sealed class SentMessageConfiguration : IEntityTypeConfiguration<SentMessage>
{
    public void Configure(EntityTypeBuilder<SentMessage> builder)
    {
        builder.ToTable("SentMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.To).IsRequired();
        builder.Property(m => m.Cc).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(m => m.Subject).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Body).IsRequired();
        builder.Property(m => m.SentAt).HasConversion(new DateTimeOffsetToBinaryConverter()).IsRequired();

        builder.HasOne<EmailAccount>().WithMany().HasForeignKey(m => m.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.AccountId, m.SentAt });
    }
}

internal sealed class MailAttachmentConfiguration : IEntityTypeConfiguration<MailAttachment>
{
    public void Configure(EntityTypeBuilder<MailAttachment> builder)
    {
        builder.ToTable("MailAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Content).IsRequired();

        builder.HasOne<ReceivedMessage>().WithMany().HasForeignKey(a => a.ReceivedMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SentMessage>().WithMany().HasForeignKey(a => a.SentMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Draft>().WithMany().HasForeignKey(a => a.DraftId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DeletedMessageConfiguration : IEntityTypeConfiguration<DeletedMessage>
{
    public void Configure(EntityTypeBuilder<DeletedMessage> builder)
    {
        builder.ToTable("DeletedMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Uid).HasMaxLength(200).IsRequired();

        builder.HasOne<EmailAccount>().WithMany().HasForeignKey(m => m.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.AccountId, m.Uid }).IsUnique();
    }
}

internal sealed class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.ToTable("Drafts");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.To).IsRequired();
        builder.Property(d => d.Cc).IsRequired();
        builder.Property(d => d.Subject).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Body).IsRequired();
        builder.Property(d => d.UpdatedAt).HasConversion(new DateTimeOffsetToBinaryConverter()).IsRequired();

        builder.HasIndex(d => new { d.UserId, d.UpdatedAt });
    }
}
