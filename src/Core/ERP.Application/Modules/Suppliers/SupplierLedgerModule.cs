using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Suppliers;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Suppliers;
using FluentValidation;

namespace ERP.Application.Modules.Suppliers;

/// <summary>Təchizatçı defteri (#15) — DTO çevirmə + növ parse.</summary>
public static class SupplierLedgerMapping
{
    public static SupplierLedgerEntryDto ToDto(this SupplierLedgerEntry e) => new(
        Id: e.Id,
        SupplierId: e.SupplierId,
        Date: e.Date,
        Type: e.Type.ToString(),
        Amount: e.Amount.Amount,
        Currency: e.Amount.Currency,
        Description: e.Description,
        HasDocument: !string.IsNullOrEmpty(e.DocumentPath));

    public static SupplierEntryType ParseType(string? type)
    {
        if (Enum.TryParse<SupplierEntryType>(type, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Defter qeydi növü düzgün deyil: {type}. (Borc | Ödəniş | Danışıq | Sənəd)");
    }
}

// --- Command: qeyd əlavə et ---
public sealed record AddSupplierEntryCommand(Guid SupplierId, AddSupplierEntryRequest Request)
    : IRequest<Result<Guid>>;

public sealed class AddSupplierEntryValidator : AbstractValidator<AddSupplierEntryCommand>
{
    public AddSupplierEntryValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Request.Type).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddSupplierEntryHandler(
    ISupplierRepository suppliers,
    ISupplierLedgerRepository ledger,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddSupplierEntryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddSupplierEntryCommand request, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(request.SupplierId, ct);
        if (supplier is null)
            return Result.Failure<Guid>($"Təchizatçı tapılmadı: {request.SupplierId}");

        try
        {
            var type = SupplierLedgerMapping.ParseType(request.Request.Type);
            var entry = SupplierLedgerEntry.Create(
                request.SupplierId,
                request.Request.Date,
                type,
                Money.Create(request.Request.Amount),
                request.Request.Description);

            await ledger.AddAsync(entry, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(entry.Id);
        }
        catch (DomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }
    }
}

// --- Query: təchizatçı defteri (qeydlər + balans) ---
public sealed record GetSupplierLedgerQuery(Guid SupplierId) : IRequest<Result<SupplierLedgerDto>>;

public sealed class GetSupplierLedgerHandler(
    ISupplierRepository suppliers,
    ISupplierLedgerRepository ledger)
    : IRequestHandler<GetSupplierLedgerQuery, Result<SupplierLedgerDto>>
{
    public async Task<Result<SupplierLedgerDto>> Handle(GetSupplierLedgerQuery request, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(request.SupplierId, ct);
        if (supplier is null)
            return Result.Failure<SupplierLedgerDto>($"Təchizatçı tapılmadı: {request.SupplierId}");

        var entries = await ledger.GetBySupplierAsync(request.SupplierId, ct);
        var totalDebt = entries.Where(e => e.Type == SupplierEntryType.Borc).Sum(e => e.Amount.Amount);
        var totalPaid = entries.Where(e => e.Type == SupplierEntryType.Ödəniş).Sum(e => e.Amount.Amount);

        var dto = new SupplierLedgerDto(
            SupplierId: supplier.Id,
            SupplierName: supplier.Name,
            TotalDebt: totalDebt,
            TotalPaid: totalPaid,
            Balance: totalDebt - totalPaid,
            Currency: "AZN",
            Entries: entries.Select(e => e.ToDto()).ToList());

        return Result.Success(dto);
    }
}
