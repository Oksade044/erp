using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Orders;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Orders;
using FluentValidation;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Yeni icarə sifarişi yaradır (Qaralama statusunda). Müştəri və məhsul adları/qiymətləri
/// snapshot kimi saxlanılır. Sətir qiyməti verilməzsə məhsulun baza qiyməti götürülür (TDD §7).
/// </summary>
public sealed record CreateOrderCommand(CreateOrderRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty().WithMessage("Sifarişdə ən azı bir sətir olmalıdır.");
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CreateOrderHandler(
    IRentalOrderRepository orders,
    ICustomerRepository customers,
    IProductRepository products,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var customer = await customers.GetByIdAsync(dto.CustomerId, ct);
        if (customer is null)
            return Result.Failure<Guid>($"Müştəri tapılmadı: {dto.CustomerId}");

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        // Sifarişi yaradan: adətən daxil olmuş istifadəçi. Amma Admin/Menecer sifarişi
        // başqa məsul əməkdaşın adına yaza bilər (JWT-dən rol yoxlanılır — spoofing olmaz).
        var isManager = currentUser.Role is "Admin" or "Menecer";
        var overrideCreator = isManager && !string.IsNullOrWhiteSpace(dto.CreatedByName);
        var createdByName = overrideCreator ? dto.CreatedByName : (currentUser.FullName ?? currentUser.UserName);
        var createdByRole = overrideCreator ? dto.CreatedByRole : currentUser.Role;

        var order = RentalOrder.Create(orderNumber, customer.Id, customer.Name, dto.StartDate, dto.EndDate, dto.Notes,
            createdByName: createdByName,
            createdByRole: createdByRole);

        foreach (var lineDto in dto.Lines)
        {
            var product = await products.GetByIdAsync(lineDto.ProductId, ct);
            if (product is null)
                return Result.Failure<Guid>($"Məhsul tapılmadı: {lineDto.ProductId}");

            // Dinamik qiymət: verilibsə onu, verilməyibsə məhsulun baza qiymətini götür.
            var unitPrice = lineDto.UnitPrice is { } p
                ? Money.Create(p, product.RentalPrice.Currency)
                : product.RentalPrice;

            order.AddLine(product.Id, product.Name, lineDto.Quantity, unitPrice);
        }

        await orders.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(order.Id);
    }
}
