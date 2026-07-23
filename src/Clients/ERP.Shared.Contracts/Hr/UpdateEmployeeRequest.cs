namespace ERP.Shared.Contracts.Hr;

/// <summary>Mövcud işçini yeniləmək üçün request DTO-su. Status: "İşləyir" | "Məzuniyyətdə" | "İşdənÇıxmış".</summary>
public sealed record UpdateEmployeeRequest(
    string FullName,
    string Position,
    string Phone,
    decimal Salary,
    string Status,
    string? Department = null,
    string? Email = null,
    string? Notes = null);
