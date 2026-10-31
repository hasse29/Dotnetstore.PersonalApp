using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Clients.Persistence;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.OrganizationNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.VatNumber).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(254);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Website).HasMaxLength(2048);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.OrganizationNumber).IsUnique();
    }
}
