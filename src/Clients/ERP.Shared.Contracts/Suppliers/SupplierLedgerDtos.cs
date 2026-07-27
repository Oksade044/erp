namespace ERP.Shared.Contracts.Suppliers;

/// <summary>Təchizatçı defter/tarixçə qeydi (#15).</summary>
public sealed record SupplierLedgerEntryDto(
    Guid Id,
    Guid SupplierId,
    DateOnly Date,
    string Type,
    decimal Amount,
    string Currency,
    string? Description,
    bool HasDocument);

/// <summary>Təchizatçı defteri — qeydlər + qalıq borc xülasəsi.</summary>
public sealed record SupplierLedgerDto(
    Guid SupplierId,
    string SupplierName,
    decimal TotalDebt,
    decimal TotalPaid,
    decimal Balance,
    string Currency,
    IReadOnlyList<SupplierLedgerEntryDto> Entries);

/// <summary>Yeni defter qeydi əlavə etmə sorğusu.</summary>
public sealed record AddSupplierEntryRequest(
    string Type,
    decimal Amount,
    DateOnly Date,
    string? Description);
