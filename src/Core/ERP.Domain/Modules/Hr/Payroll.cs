using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Hr;

/// <summary>
/// Əməkhaqqı hesablaması — aggregate root (TDD §13). Bir işçinin bir ay üçün maaşı:
/// baza maaş (işçidən snapshot) + bonus − tutulma = net maaş. Ödənildikdə Maliyyə
/// modulunda məxaric əməliyyatı yaradılır (handler səviyyəsində, cross-aggregate).
/// Bir işçi üçün bir dövrdə (il+ay) yalnız bir hesablama ola bilər.
/// </summary>
public class Payroll : BaseEntity, IAggregateRoot
{
    public string PayrollNumber { get; private set; } = null!;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = null!;

    public int Year { get; private set; }
    public int Month { get; private set; }

    public Money BaseSalary { get; private set; } = null!;
    public Money Bonus { get; private set; } = null!;
    public Money Deduction { get; private set; } = null!;

    public PayrollStatus Status { get; private set; } = PayrollStatus.Hesablanmış;
    public DateOnly? PaidDate { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Net maaş = baza + bonus − tutulma.</summary>
    public Money NetSalary => BaseSalary.Add(Bonus).Subtract(Deduction);

    // EF Core üçün.
    private Payroll() { }

    private Payroll(string number, Guid employeeId, string employeeName, int year, int month,
        Money baseSalary, Money bonus, Money deduction)
    {
        PayrollNumber = number;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        Year = year;
        Month = month;
        BaseSalary = baseSalary;
        Bonus = bonus;
        Deduction = deduction;
    }

    public static Payroll Create(
        string number,
        Guid employeeId,
        string employeeName,
        int year,
        int month,
        Money baseSalary,
        Money bonus,
        Money deduction)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Əməkhaqqı nömrəsi tələb olunur.");
        if (employeeId == Guid.Empty)
            throw new DomainException("Əməkhaqqı üçün işçi tələb olunur.");
        if (string.IsNullOrWhiteSpace(employeeName))
            throw new DomainException("İşçi adı tələb olunur.");
        if (month is < 1 or > 12)
            throw new DomainException("Ay 1 ilə 12 arasında olmalıdır.");
        if (year < 2000)
            throw new DomainException("İl düzgün deyil.");

        // Net maaşın mənfi olmamasını təmin et (tutulma baza+bonusu keçə bilməz).
        if (deduction.Amount > baseSalary.Amount + bonus.Amount)
            throw new DomainException("Tutulma baza maaş və bonusun cəmindən çox ola bilməz.");

        return new Payroll(number, employeeId, employeeName.Trim(), year, month, baseSalary, bonus, deduction);
    }

    public void MarkPaid(DateOnly date)
    {
        if (Status != PayrollStatus.Hesablanmış)
            throw new DomainException("Yalnız hesablanmış əməkhaqqı ödənilə bilər.");
        Status = PayrollStatus.Ödənilmiş;
        PaidDate = date;
    }

    public void SetNotes(string? notes) => Notes = notes?.Trim();
}
