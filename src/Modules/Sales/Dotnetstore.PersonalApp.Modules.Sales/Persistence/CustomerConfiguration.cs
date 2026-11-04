using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).HasMaxLength(25).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientId);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Number).IsUnique();
    }
}