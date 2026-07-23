using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Application.Modules.Finance;
using ERP.Shared.Contracts.Finance;

namespace ERP.Application.Modules.Finance.Queries;

/// <summary>Id-yə görə tək maliyyə əməliyyatı qaytarır (TDD §17 — Query).</summary>
public sealed record GetTransactionByIdQuery(Guid Id) : IRequest<Result<TransactionDto>>;

public sealed class GetTransactionByIdHandler(IFinancialTransactionRepository transactions)
    : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken ct)
    {
        var t = await transactions.GetByIdAsync(request.Id, ct);
        return t is null
            ? Result.Failure<TransactionDto>($"Əməliyyat tapılmadı: {request.Id}")
            : Result.Success(t.ToDto());
    }
}
