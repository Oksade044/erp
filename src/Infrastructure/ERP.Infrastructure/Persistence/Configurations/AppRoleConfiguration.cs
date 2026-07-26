using ERP.Domain.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>AppRole konfiqurasiyası (#16) — rol adı unikal, icazələr CSV.</summary>
public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.PermissionsCsv).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.IsSystem).IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
