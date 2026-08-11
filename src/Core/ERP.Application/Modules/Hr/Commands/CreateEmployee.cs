using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Hr;
using ERP.Domain.Modules.Users;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Hr;
using FluentValidation;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>Yeni işçi yaradır (TDD §17 — Command). Uğurda Id qaytarır.</summary>
public sealed record CreateEmployeeCommand(CreateEmployeeRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.Request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Phone).NotEmpty();
        RuleFor(x => x.Request.Salary).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateEmployeeHandler(
    IEmployeeRepository employees,
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        var phone = PhoneNumber.Create(dto.Phone);

        if (await employees.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure<Guid>($"Bu telefon nömrəsi ilə işçi artıq mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);
        var salary = Money.Create(dto.Salary);
        var number = $"EMP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var employee = Employee.Create(
            number, dto.FullName, dto.Position, phone, dto.HireDate, salary, dto.Department, email, dto.Notes);

        await employees.AddAsync(employee, ct);

        // Şifrə verilibsə — işçi üçün sistem/mobil giriş (User) də yarat (eyni transaksiya).
        // Belə ki, işçi Android/iOS tətbiqinə öz adı ilə daxil ola bilsin.
        if (!string.IsNullOrWhiteSpace(dto.LoginPassword))
        {
            var username = (string.IsNullOrWhiteSpace(dto.LoginUsername) ? phone.Value : dto.LoginUsername)
                .Trim().ToLowerInvariant();

            if (await users.GetByUsernameAsync(username, ct) is not null)
                return Result.Failure<Guid>($"Bu istifadəçi adı artıq mövcuddur: {username}");

            var roleName = string.IsNullOrWhiteSpace(dto.LoginRole) ? Role.Kassir.ToString() : dto.LoginRole.Trim();
            if (await roles.GetByNameAsync(roleName, ct) is null)
                return Result.Failure<Guid>($"Belə rol yoxdur: {roleName}");

            var (hash, salt) = passwordHasher.Hash(dto.LoginPassword);
            var user = User.Create(username, hash, salt, dto.FullName, roleName);
            await users.AddAsync(user, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(employee.Id);
    }
}
