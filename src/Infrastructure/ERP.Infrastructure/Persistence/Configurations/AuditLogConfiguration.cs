using ERP.Domain.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>AuditLog konfiqurasiyası (#26). Sorğu üçün tarix + istifadəçi indeksli.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(30).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Summary).HasMaxLength(2000);

        builder.HasIndex(a => a.TimestampTicks);
        builder.HasIndex(a => a.EntityType);
    }
}
