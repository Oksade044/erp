using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Suppliers.Commands;

/// <summary>Təchizatçını silir (soft delete).</summary>
public sealed record DeleteSupplierCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteSupplierHandler(ISupplierRepository suppliers, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSupplierCommand, Result>
{
    public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(request.Id, ct);
        if (supplier is null) return Result.Failure($"Təchizatçı tapılmadı: {request.Id}");
        suppliers.Remove(supplier);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
