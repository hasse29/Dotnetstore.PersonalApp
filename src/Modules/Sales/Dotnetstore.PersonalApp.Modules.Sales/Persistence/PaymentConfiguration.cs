using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("InvoicePayments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(14, 2);
        builder.Property(p => p.Excess).HasPrecision(14, 2);
        builder.Property(p => p.Note).HasMaxLength(300);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.InvoiceId);
    }
}

internal sealed class CreditEntryConfiguration : IEntityTypeConfiguration<CreditEntry>
{
    public void Configure(EntityTypeBuilder<CreditEntry> builder)
    {
        builder.ToTable("CreditEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(14, 2);
        builder.Property(e => e.Note).HasMaxLength(300);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.PaymentId);

        // Credit is money owed to the customer: the customer cannot be deleted while there is a ledger for them.
        builder.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
