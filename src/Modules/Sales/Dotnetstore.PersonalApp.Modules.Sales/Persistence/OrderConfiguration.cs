using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Number).HasMaxLength(30).IsRequired().UseCollation("NOCASE");
        builder.Property(o => o.OfferNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CustomerAddress).HasMaxLength(400);
        builder.Property(o => o.Reference).HasMaxLength(200);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => o.Number).IsUnique();
        builder.HasIndex(o => o.OfferId);

        // Orders keep their offer and customer as history: neither can be deleted while an order refers to it.
        builder.HasOne<Offer>().WithMany().HasForeignKey(o => o.OfferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.PickList).WithOne().HasForeignKey<PickList>(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Unit).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(12, 2);
        builder.Property(l => l.VatRate).HasPrecision(5, 2);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);

        builder.HasIndex(l => new { l.OrderId, l.Position });
    }
}

internal sealed class PickListConfiguration : IEntityTypeConfiguration<PickList>
{
    public void Configure(EntityTypeBuilder<PickList> builder)
    {
        builder.ToTable("PickLists");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Number).HasMaxLength(30).IsRequired().UseCollation("NOCASE");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.Number).IsUnique();
        builder.HasIndex(p => p.OrderId).IsUnique();

        builder.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PickListId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PickListLineConfiguration : IEntityTypeConfiguration<PickListLine>
{
    public void Configure(EntityTypeBuilder<PickListLine> builder)
    {
        builder.ToTable("PickListLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Unit).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.PickedQuantity).HasPrecision(12, 2);

        builder.HasIndex(l => new { l.PickListId, l.Position });

        builder.HasMany(l => l.Allocations).WithOne().HasForeignKey(a => a.PickListLineId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PickAllocationConfiguration : IEntityTypeConfiguration<PickAllocation>
{
    public void Configure(EntityTypeBuilder<PickAllocation> builder)
    {
        builder.ToTable("PickAllocations");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Quantity).HasPrecision(12, 2);

        builder.HasIndex(a => a.PickListLineId);
    }
}
