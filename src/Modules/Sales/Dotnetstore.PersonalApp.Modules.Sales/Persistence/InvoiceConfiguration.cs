using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number).HasMaxLength(30).IsRequired().UseCollation("NOCASE");
        builder.Property(i => i.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.CustomerAddress).HasMaxLength(400);
        builder.Property(i => i.Reference).HasMaxLength(200);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.OrderId);

        // Invoices are history: neither their order nor their customer can be deleted while an invoice refers to it.
        builder.HasOne<Order>().WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(i => i.Lines).WithOne().HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Payments).WithOne().HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Unit).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(12, 2);
        builder.Property(l => l.UnitPrice).HasPrecision(12, 2);
        builder.Property(l => l.VatRate).HasPrecision(5, 2);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);

        builder.HasIndex(l => new { l.InvoiceId, l.Position });
    }
}