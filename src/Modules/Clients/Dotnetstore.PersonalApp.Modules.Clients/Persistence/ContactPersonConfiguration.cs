using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Clients.Persistence;

internal sealed class ContactPersonConfiguration : IEntityTypeConfiguration<ContactPerson>
{
    public void Configure(EntityTypeBuilder<ContactPerson> builder)
    {
        builder.ToTable("ContactPersons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.JobTitle).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasOne<Client>().WithMany().HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.ClientId);
    }
}
