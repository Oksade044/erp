using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Hr;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr;

/// <summary>Employee entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class EmployeeMapping
{
    public static EmployeeDto ToDto(this Employee e) => new(
        Id: e.Id,
        EmployeeNumber: e.EmployeeNumber,
        FullName: e.FullName,
        Position: e.Position,
        Department: e.Department,
        Phone: e.Phone.Value,
        Email: e.Email?.Value,
        HireDate: e.HireDate,
        Salary: e.Salary.Amount,
        Currency: e.Salary.Currency,
        Status: e.Status.ToString(),
        Notes: e.Notes,
        CreatedAt: e.CreatedAt);

    public static EmployeeStatus ParseStatus(string? status)
    {
        if (Enum.TryParse<EmployeeStatus>(status, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"İşçi statusu düzgün deyil: {status}. (İşləyir | Məzuniyyətdə | İşdənÇıxmış)");
    }
}
