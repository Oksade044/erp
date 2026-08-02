using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Customers;

namespace ERP.Application.Modules.Customers.Queries;

/// <summary>
/// Kartında ilkin borcu (Debt > 0) olan bütün müştəriləri qaytarır — Borclar bölməsi üçün.
/// Səhifələmir: borclu müştəri sayı adətən azdır, hamısı bir dəfəyə lazımdır.
/// </summary>
public sealed record GetCustomerDebtsQuery : IRequest<List<CustomerDto>>;

public sealed class GetCustomerDebtsHandler(ICustomerRepository customers)
    : IRequestHandler<GetCustomerDebtsQuery, List<CustomerDto>>
{
    public async Task<List<CustomerDto>> Handle(GetCustomerDebtsQuery request, CancellationToken ct)
    {
        var debtors = await customers.GetDebtorsAsync(ct);
        return debtors.Select(c => c.ToDto()).ToList();
    }
}
