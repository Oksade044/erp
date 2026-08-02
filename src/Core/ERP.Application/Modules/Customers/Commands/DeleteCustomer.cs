using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Customers.Commands;

/// <summary>Müştərini silir (soft delete).</summary>
public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCustomerHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCustomerCommand, Result>
{
    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.Id, ct);
        if (customer is null) return Result.Failure($"Müştəri tapılmadı: {request.Id}");
        customers.Remove(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
