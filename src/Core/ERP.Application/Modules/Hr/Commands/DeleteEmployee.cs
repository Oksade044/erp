using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>İşçini (təmsilçini) silir (soft delete).</summary>
public sealed record DeleteEmployeeCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteEmployeeHandler(IEmployeeRepository employees, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEmployeeCommand, Result>
{
    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(request.Id, ct);
        if (employee is null) return Result.Failure($"İşçi tapılmadı: {request.Id}");
        employees.Remove(employee);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
