using Dotnetstore.PersonalApp.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnetstore.PersonalApp.Modules.Audit.Persistence;

internal sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("AuditRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Timestamp).IsRequired();
        builder.Property(r => r.Username).HasMaxLength(100);
        builder.Property(r => r.Operation).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ErrorCode).HasMaxLength(200);
        builder.Property(r => r.Fields).HasMaxLength(1000);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.EntityId);
    }
}
