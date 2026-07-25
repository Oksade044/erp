namespace ERP.Shared.Contracts.Settings;

/// <summary>Bir həssas sahənin görünürlük qaydası — hansı rollar görə bilir.</summary>
public sealed record FieldPermissionDto(
    string FieldKey,
    string DisplayName,
    IReadOnlyList<string> AllowedRoles);

/// <summary>Sahə görünürlüyünü yeniləmək üçün request.</summary>
public sealed record UpdateFieldPermissionRequest(
    string FieldKey,
    IReadOnlyList<string> AllowedRoles);
