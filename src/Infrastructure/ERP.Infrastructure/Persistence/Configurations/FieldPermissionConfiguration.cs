using ERP.Domain.Modules.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>FieldPermission konfiqurasiyası — FieldKey unikal, rollar CSV kimi (TDD §14).</summary>
public sealed class FieldPermissionConfiguration : IEntityTypeConfiguration<FieldPermission>
{
    public void Configure(EntityTypeBuilder<FieldPermission> builder)
    {
        builder.ToTable("FieldPermissions");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FieldKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(f => f.FieldKey).IsUnique();

        builder.Property(f => f.AllowedRolesCsv).HasMaxLength(200).IsRequired();

        builder.Ignore(f => f.AllowedRoles);

        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}
