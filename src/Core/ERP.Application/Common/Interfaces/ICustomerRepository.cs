using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Müştəriyə xas repository (TDD §14). Ümumi IRepository-ni domenə xas sorğularla genişləndirir.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(string normalizedPhone, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default);

    /// <summary>Server-side axtarış + səhifələmə (TDD §11, §33).</summary>
    Task<PagedResult<Customer>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Kartında ilkin borcu olan (Debt owned VO non-null) BÜTÜN müştərilər — Borclar bölməsi üçün.
    /// Səhifələmədən asılı deyil ki, çox müştəri olduqda borclu 20-lik səhifədən kənarda qalmasın.
    /// </summary>
    Task<IReadOnlyList<Customer>> GetDebtorsAsync(CancellationToken ct = default);
}
