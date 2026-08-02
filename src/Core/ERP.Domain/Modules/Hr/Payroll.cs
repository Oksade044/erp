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

    private readonly List<PayrollPayment> _payments = [];
    /// <summary>Hissə-hissə ödənişlər (installment) və bonus qeydləri — yalnız oxu (tarixçə).</summary>
    public IReadOnlyList<PayrollPayment> Payments => _payments.AsReadOnly();

    /// <summary>
    /// İndiyədək ödənilmiş məbləğ (installment-lərin cəmi) — saxlanan sütun. Ödəniş qeydləri
    /// ayrıca (standalone) yazılır; buna görə bu dəyər domendə birbaşa saxlanılır.
    /// </summary>
    public Money PaidAmount { get; private set; } = null!;

    /// <summary>Net maaş = baza + bonus − tutulma.</summary>
    public Money NetSalary => BaseSalary.Add(Bonus).Subtract(Deduction);

    /// <summary>Qalıq borc = net maaş − ödənilmiş (0-dan aşağı düşməz).</summary>
    public Money Remaining
    {
        get
        {
            var rem = NetSalary.Amount - PaidAmount.Amount;
            return Money.Create(rem < 0m ? 0m : rem, BaseSalary.Currency);
        }
    }

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
        PaidAmount = Money.Zero(baseSalary.Currency);
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

    /// <summary>Qalan borcu birdəfəyə ödəyir (tam ödəniş — installment-in xüsusi halı).</summary>
    public PayrollPayment MarkPaid(DateOnly date)
    {
        if (Status == PayrollStatus.Ödənilmiş)
            throw new DomainException("Əməkhaqqı artıq tam ödənilib.");
        return AddPayment(Remaining, date, "Köçürmə", "Tam ödəniş");
    }

    /// <summary>
    /// Hissə-hissə ödəniş qeyd edir (installment). Məbləğ qalıq borcdan çox ola bilməz.
    /// Ödənilmiş məbləği (saxlanan) artırır və statusu yeniləyir. Yaradılan PayrollPayment
    /// qaytarılır — handler onu ayrıca (standalone) yazır və Maliyyəyə məxaric əlavə edir.
    /// </summary>
    public PayrollPayment AddPayment(Money amount, DateOnly date, string method, string? note)
    {
        if (amount.Amount <= 0m)
            throw new DomainException("Ödəniş məbləği müsbət olmalıdır.");
        if (amount.Amount > Remaining.Amount)
            throw new DomainException($"Ödəniş qalıq borcdan ({Remaining.Amount:0.00}) çox ola bilməz.");

        PaidAmount = PaidAmount.Add(amount);

        if (Remaining.Amount <= 0m)
        {
            Status = PayrollStatus.Ödənilmiş;
            PaidDate = date;
        }
        else
        {
            Status = PayrollStatus.QismənÖdənilmiş;
        }
        return new PayrollPayment(Id, amount, date, method, note, isBonus: false);
    }

    /// <summary>
    /// Aya əlavə bonus verir — bu ayın bonusunu (net maaşı) artırır. Yaradılan bonus qeydini
    /// qaytarır — handler onu ayrıca yazır və Maliyyəyə məxaric əlavə edir.
    /// </summary>
    public PayrollPayment AddBonus(Money extra, DateOnly date, string method, string? note)
    {
        if (extra.Amount <= 0m)
            throw new DomainException("Bonus məbləği müsbət olmalıdır.");
        Bonus = Bonus.Add(extra);
        return new PayrollPayment(Id, extra, date, method, note ?? "Bonus", isBonus: true);
    }

    public void SetNotes(string? notes) => Notes = notes?.Trim();
}
