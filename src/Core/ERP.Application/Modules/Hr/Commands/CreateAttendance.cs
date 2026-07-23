using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;
using ERP.Shared.Contracts.Hr;
using FluentValidation;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>
/// Yeni davamiyyət qeydi yaradır. İşçi adı snapshot kimi saxlanılır; bir işçi üçün bir gündə
/// yalnız bir qeydə icazə verilir.
/// </summary>
public sealed record CreateAttendanceCommand(CreateAttendanceRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateAttendanceValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.Status).NotEmpty();
    }
}

public sealed class CreateAttendanceHandler(
    IAttendanceRepository attendance,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAttendanceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAttendanceCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var employee = await employees.GetByIdAsync(dto.EmployeeId, ct);
        if (employee is null)
            return Result.Failure<Guid>($"İşçi tapılmadı: {dto.EmployeeId}");

        if (await attendance.ExistsForEmployeeDateAsync(dto.EmployeeId, dto.Date, ct))
            return Result.Failure<Guid>(
                $"Bu işçi üçün {dto.Date:yyyy-MM-dd} tarixində davamiyyət qeydi artıq var.");

        var status = AttendanceMapping.ParseStatus(dto.Status);
        var record = Attendance.Create(
            employee.Id, employee.FullName, dto.Date, status, dto.CheckIn, dto.CheckOut, dto.Notes);

        await attendance.AddAsync(record, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(record.Id);
    }
}
