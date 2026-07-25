namespace ERP.Shared.Contracts.Products;

/// <summary>Kateqoriya cavab DTO-su.</summary>
public sealed record CategoryDto(Guid Id, string Name, bool IsActive);

/// <summary>Yeni kateqoriya yaratmaq üçün request.</summary>
public sealed record CreateCategoryRequest(string Name);
