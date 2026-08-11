namespace ERP.Shared.Contracts.Hr;

/// <summary>Yeni işçi yaratmaq üçün request DTO-su.</summary>
public sealed record CreateEmployeeRequest(
    string FullName,
    string Position,
    string Phone,
    DateOnly HireDate,
    decimal Salary,
    string? Department = null,
    string? Email = null,
    string? Notes = null,
    // Mobil/sistem girişi — doldurulsa işçi üçün login (User) da yaradılır.
    // LoginUsername boş olsa telefon istifadə olunur; LoginRole boş olsa "Kassir".
    string? LoginUsername = null,
    string? LoginPassword = null,
    string? LoginRole = null);
