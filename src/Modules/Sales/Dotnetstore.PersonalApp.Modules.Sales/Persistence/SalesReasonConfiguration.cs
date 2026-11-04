using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence;

internal sealed class SalesReasonConfiguration : IEntityTypeConfiguration<SalesReason>
{
    public void Configure(EntityTypeBuilder<SalesReason> builder)
    {
        builder.ToTable("SalesReasons");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.Property(x => x.ReasonType).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new SalesReason { Id = new Guid("3523bb57-6f80-2491-bc3a-be499d9b56ac"), Name = "Price", ReasonType = "O" },
            new SalesReason { Id = new Guid("d4f1595c-c150-a1aa-37f1-65ac410700bd"), Name = "On promotion", ReasonType = "P" },
            new SalesReason { Id = new Guid("401c4639-8946-3b6e-2b8a-c36946b8ed0a"), Name = "Magazine advertisement", ReasonType = "M" },
            new SalesReason { Id = new Guid("d96f4fa3-020f-b872-fd91-2af0b37bc2ce"), Name = "Television advertisement", ReasonType = "M" },
            new SalesReason { Id = new Guid("6105de8d-013e-972b-543f-dfeb45fe6334"), Name = "Quality", ReasonType = "O" },
            new SalesReason { Id = new Guid("8435cc86-4e64-0c58-5596-37f4c5ff64f0"), Name = "Review", ReasonType = "O" },
            new SalesReason { Id = new Guid("962c1314-03d2-7f46-6ce2-0f8efd152dca"), Name = "Other", ReasonType = "O" });
    }
}