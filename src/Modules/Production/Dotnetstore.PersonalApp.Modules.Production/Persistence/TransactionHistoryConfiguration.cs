using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class TransactionHistoryConfiguration : IEntityTypeConfiguration<TransactionHistory>
{
    public void Configure(EntityTypeBuilder<TransactionHistory> builder)
    {
        builder.ToTable("TransactionHistories");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ReferenceOrder).HasMaxLength(25);
        builder.Property(x => x.TransactionDate).IsRequired();
        builder.Property(x => x.TransactionType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.ActualCost).IsRequired();
    }
}