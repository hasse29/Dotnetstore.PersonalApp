using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence;

internal sealed class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("EmailAccounts");
        builder.HasKey(a => a.Id);
        builder.Ignore(a => a.IsSystem);

        // NOCASE makes the unique index (and name comparisons) ignore case: "work" clashes with "Work".
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        builder.Property(a => a.Protocol).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(a => a.Host).HasMaxLength(255).IsRequired();
        builder.Property(a => a.Port).IsRequired();
        builder.Property(a => a.Security).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.EmailAddress).HasMaxLength(254).IsRequired();
        builder.Property(a => a.Username).HasMaxLength(200);
        builder.Property(a => a.ProtectedPassword).HasMaxLength(2000);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.OwnerId, a.Name }).IsUnique();
    }
}
