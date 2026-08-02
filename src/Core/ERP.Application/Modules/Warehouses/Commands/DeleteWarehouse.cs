using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Warehouses.Commands;

/// <summary>Anbarı silir (soft delete).</summary>
public sealed record DeleteWarehouseCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteWarehouseHandler(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteWarehouseCommand, Result>
{
    public async Task<Result> Handle(DeleteWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(request.Id, ct);
        if (warehouse is null) return Result.Failure($"Anbar tapılmadı: {request.Id}");
        warehouses.Remove(warehouse);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
