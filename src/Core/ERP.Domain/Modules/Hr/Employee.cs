using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Hr;

/// <summary>
/// İşçi — aggregate root, rich domain model (TDD §13). HR bloku üçün əsas entity;
/// Əməkhaqqı (Payroll) və Davamiyyət (Attendance) modulları buna istinad edəcək.
/// Value object-lər (PhoneNumber/Email) Customers modulundan təkrar-istifadə olunur (DRY).
/// </summary>
public class Employee : BaseEntity, IAggregateRoot
{
    public string EmployeeNumber { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string Position { get; private set; } = null!;
    public string? Department { get; private set; }

    public PhoneNumber Phone { get; private set; } = null!;
    public Email? Email { get; private set; }

    public DateOnly HireDate { get; private set; }

    /// <summary>Aylıq baza əməkhaqqı (Payroll hesablamalarında istifadə olunur).</summary>
    public Money Salary { get; private set; } = null!;

    public EmployeeStatus Status { get; private set; } = EmployeeStatus.İşləyir;
    public string? Notes { get; private set; }

    // EF Core üçün.
    private Employee() { }

    private Employee(string number, string fullName, string position, PhoneNumber phone, DateOnly hireDate, Money salary)
    {
        EmployeeNumber = number;
        FullName = fullName;
        Position = position;
        Phone = phone;
        HireDate = hireDate;
        Salary = salary;
    }

    public static Employee Create(
        string number,
        string fullName,
        string position,
        PhoneNumber phone,
        DateOnly hireDate,
        Money salary,
        string? department = null,
        Email? email = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("İşçi nömrəsi tələb olunur.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("İşçinin adı tələb olunur.");
        if (string.IsNullOrWhiteSpace(position))
            throw new DomainException("Vəzifə tələb olunur.");

        return new Employee(number, fullName.Trim(), position.Trim(), phone, hireDate, salary)
        {
            Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            Email = email,
            Notes = notes?.Trim()
        };
    }

    public void UpdateDetails(string fullName, string position, string? department, PhoneNumber phone, Email? email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("İşçinin adı tələb olunur.");
        if (string.IsNullOrWhiteSpace(position))
            throw new DomainException("Vəzifə tələb olunur.");

        FullName = fullName.Trim();
        Position = position.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        Phone = phone ?? throw new DomainException("Telefon nömrəsi tələb olunur.");
        Email = email;
    }

    public void ChangeSalary(Money salary) =>
        Salary = salary ?? throw new DomainException("Əməkhaqqı tələb olunur.");

    public void SetStatus(EmployeeStatus status) => Status = status;
    public void SetNotes(string? notes) => Notes = notes?.Trim();
}
