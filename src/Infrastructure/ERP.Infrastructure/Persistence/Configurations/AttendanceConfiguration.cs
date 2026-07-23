using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Attendance konfiqurasiyası. Bir işçi üçün bir gündə bir qeyd (unikal indeks EmployeeId+Date).
/// Hesablanan WorkedHours map olunmur. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Date).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(a => a.WorkedHours);

        // Bir işçi üçün bir gündə yalnız bir davamiyyət qeydi.
        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
