using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Finance;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Finance;
using FluentValidation;

namespace ERP.Application.Modules.Finance.Commands;

/// <summary>Yeni maliyyə əməliyyatı (mədaxil/məxaric) yaradır (TDD §17 — Command).</summary>
public sealed record CreateTransactionCommand(CreateTransactionRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Request.Type).NotEmpty();
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Method).NotEmpty();
    }
}

public sealed class CreateTransactionHandler(
    IFinancialTransactionRepository transactions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTransactionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var type = FinanceMapping.ParseType(dto.Type);
        var method = FinanceMapping.ParseMethod(dto.Method);
        var amount = Money.Create(dto.Amount);

        var number = $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var transaction = FinancialTransaction.Create(
            number, type, dto.Category, amount, dto.Date, method, dto.Description);

        await transactions.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(transaction.Id);
    }
}
