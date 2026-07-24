namespace ERP.Shared.Contracts.Orders;

/// <summary>Sifarişə depozit/girov məbləğini təyin etmək üçün request DTO-su.</summary>
public sealed record SetDepositRequest(decimal Deposit);
