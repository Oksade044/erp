using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Employee konfiqurasiyası. Phone/Email owned VO, Salary owned Money. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Position).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.HireDate).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(e => e.Phone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("Phone").HasMaxLength(20).IsRequired();
            phone.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(e => e.Email, email =>
        {
            email.Property(m => m.Value).HasColumnName("Email").HasMaxLength(256);
        });

        builder.OwnsOne(e => e.Salary, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Salary").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
