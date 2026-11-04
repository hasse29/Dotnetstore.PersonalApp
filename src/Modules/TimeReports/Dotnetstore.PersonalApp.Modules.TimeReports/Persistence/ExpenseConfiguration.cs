using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.VatAmount).HasPrecision(12, 2);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.Date });

        builder.HasOne(e => e.Receipt)
            .WithOne()
            .HasForeignKey<ExpenseReceipt>(r => r.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExpenseReceiptConfiguration : IEntityTypeConfiguration<ExpenseReceipt>
{
    public void Configure(EntityTypeBuilder<ExpenseReceipt> builder)
    {
        builder.ToTable("ExpenseReceipts");
        builder.HasKey(r => r.ExpenseId);
        builder.Property(r => r.FileName).HasMaxLength(260).IsRequired();
        builder.Property(r => r.Content).IsRequired();
    }
}
