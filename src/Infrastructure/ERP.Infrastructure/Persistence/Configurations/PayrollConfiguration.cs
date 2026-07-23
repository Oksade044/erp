using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Payroll konfiqurasiyası. BaseSalary/Bonus/Deduction owned Money; hesablanan NetSalary
/// map olunmur. Bir işçi üçün bir dövrdə bir hesablama (unikal EmployeeId+Year+Month).
/// Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> builder)
    {
        builder.ToTable("Payrolls");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PayrollNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.PayrollNumber).IsUnique();

        builder.Property(p => p.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Month).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(p => p.BaseSalary, m =>
        {
            m.Property(x => x.Amount).HasColumnName("BaseSalary").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(p => p.Bonus, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Bonus").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("BonusCurrency").HasMaxLength(3).IsRequired();
        });
        builder.OwnsOne(p => p.Deduction, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Deduction").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("DeductionCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(p => p.NetSalary);

        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
