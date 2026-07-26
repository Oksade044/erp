using ERP.Domain.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>User EF Core konfiqurasiyası. Parol yalnız hash+salt kimi saxlanılır.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordSalt).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(200);
        builder.Property(u => u.IsActive).IsRequired();

        // #16 — rol adı string kimi; köhnə "Role" sütununda qalır (data migration lazım deyil).
        builder.Property(u => u.RoleName)
            .HasColumnName("Role")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
