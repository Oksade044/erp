using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;
using FluentValidation;

namespace ERP.Application.Modules.Warehouses.Commands;

/// <summary>
/// Bir məhsulun müəyyən miqdarını bir anbardan başqasına köçürür: mənbə səviyyəsindən azaldır
/// (kifayət qədər olmalıdır), təyinat səviyyəsinə artırır (yoxdursa yaradır). Tək transaction (TDD §15).
/// </summary>
public sealed record TransferStockCommand(TransferStockRequest Request) : IRequest<Result>;

public sealed class TransferStockValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockValidator()
    {
        RuleFor(x => x.Request.ProductId).NotEmpty();
        RuleFor(x => x.Request.FromWarehouseId).NotEmpty();
        RuleFor(x => x.Request.ToWarehouseId).NotEmpty();
        RuleFor(x => x.Request.Quantity).GreaterThan(0);
        RuleFor(x => x.Request)
            .Must(r => r.FromWarehouseId != r.ToWarehouseId)
            .WithMessage("Mənbə və təyinat anbarı eyni ola bilməz.");
    }
}

public sealed class TransferStockHandler(
    IStockLevelRepository stockLevels,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TransferStockCommand, Result>
{
    public async Task<Result> Handle(TransferStockCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var source = await stockLevels.GetAsync(dto.ProductId, dto.FromWarehouseId, ct);
        if (source is null)
            return Result.Failure("Mənbə anbarda bu məhsul üçün stok yoxdur.");

        // Mənbədən azalt (kifayət qədər stok yoxdursa domain xəta atır).
        try
        {
            source.Decrease(dto.Quantity);
        }
        catch (ERP.Domain.Exceptions.DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
        stockLevels.Update(source);

        // Təyinata artır (səviyyə yoxdursa yarat — məhsul adı mənbədən, anbar adı anbardan).
        var dest = await stockLevels.GetAsync(dto.ProductId, dto.ToWarehouseId, ct);
        if (dest is null)
        {
            var toWarehouse = await warehouses.GetByIdAsync(dto.ToWarehouseId, ct);
            if (toWarehouse is null)
                return Result.Failure($"Təyinat anbarı tapılmadı: {dto.ToWarehouseId}");

            dest = StockLevel.Create(dto.ProductId, source.ProductName, toWarehouse.Id, toWarehouse.Name);
            dest.Increase(dto.Quantity);
            await stockLevels.AddAsync(dest, ct);
        }
        else
        {
            dest.Increase(dto.Quantity);
            stockLevels.Update(dest);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
