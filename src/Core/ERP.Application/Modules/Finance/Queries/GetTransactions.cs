using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Application.Modules.Finance;
using ERP.Domain.Modules.Finance;
using ERP.Shared.Contracts.Finance;

namespace ERP.Application.Modules.Finance.Queries;

/// <summary>Maliyyə əməliyyatlarını axtarış + növ filtri + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetTransactionsQuery(string? Search, string? Type, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<TransactionDto>>;

public sealed class GetTransactionsHandler(IFinancialTransactionRepository transactions)
    : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        TransactionType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type)
            && Enum.TryParse<TransactionType>(request.Type, ignoreCase: true, out var parsed))
            type = parsed;

        var result = await transactions.SearchAsync(request.Search, type, page, size, ct);

        return new PagedResult<TransactionDto>
        {
            Items = result.Items.Select(t => t.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
