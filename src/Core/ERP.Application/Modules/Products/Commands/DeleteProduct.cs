using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Məhsulu silir (soft delete — AuditInterceptor hard delete-i soft-a çevirir).</summary>
public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(request.Id, ct);
        if (product is null) return Result.Failure($"Məhsul tapılmadı: {request.Id}");
        products.Remove(product);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
