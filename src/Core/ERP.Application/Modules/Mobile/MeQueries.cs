using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Application.Modules.Orders;
using ERP.Shared.Contracts.Mobile;
using ERP.Shared.Contracts.Orders;

namespace ERP.Application.Modules.Mobile;

/// <summary>
/// Mobil işçi tətbiqi üçün "mən"ə xas oxumalar (TDD §7, §17). Bütün nəticələr cari
/// istifadəçinin adına (CreatedByName snapshot) görə süzülür — işçi yalnız öz işini görür.
/// </summary>

// --- Mənim dashboard-um ---
public sealed record GetMyDashboardQuery : IRequest<Result<EmployeeDashboardDto>>;

public sealed class GetMyDashboardHandler(IReportService reports, ICurrentUser currentUser)
    : IRequestHandler<GetMyDashboardQuery, Result<EmployeeDashboardDto>>
{
    public async Task<Result<EmployeeDashboardDto>> Handle(GetMyDashboardQuery request, CancellationToken ct)
    {
        var name = currentUser.FullName ?? currentUser.UserName;
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<EmployeeDashboardDto>("İstifadəçi müəyyən edilmədi.");
        return Result.Success(await reports.GetEmployeeDashboardAsync(name, ct));
    }
}

// --- Mənim maliyyəm ---
public sealed record GetMyFinanceQuery : IRequest<Result<EmployeeFinanceDto>>;

public sealed class GetMyFinanceHandler(IReportService reports, ICurrentUser currentUser)
    : IRequestHandler<GetMyFinanceQuery, Result<EmployeeFinanceDto>>
{
    public async Task<Result<EmployeeFinanceDto>> Handle(GetMyFinanceQuery request, CancellationToken ct)
    {
        var name = currentUser.FullName ?? currentUser.UserName;
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<EmployeeFinanceDto>("İstifadəçi müəyyən edilmədi.");
        return Result.Success(await reports.GetEmployeeFinanceAsync(name, ct));
    }
}

// --- Mənim sifarişlərim (gün/status filtri ilə) ---
public sealed record GetMyOrdersQuery(string? Filter = "all") : IRequest<Result<IReadOnlyList<OrderDto>>>;

public sealed class GetMyOrdersHandler(IRentalOrderRepository orders, ICurrentUser currentUser)
    : IRequestHandler<GetMyOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var name = currentUser.FullName ?? currentUser.UserName;
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<IReadOnlyList<OrderDto>>("İstifadəçi müəyyən edilmədi.");

        // İşçi sifariş həcmi məhduddur — böyük səhifə götürüb ada və filtrə görə süzürük.
        var page = await orders.SearchAsync(null, 1, 500, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var mine = page.Items.Where(o => o.CreatedByName == name);

        mine = (request.Filter ?? "all").ToLowerInvariant() switch
        {
            "today-delivery" => mine.Where(o => o.StartDate == today && o.Status == Domain.Modules.Orders.OrderStatus.Təsdiqlənmiş),
            "today-return"   => mine.Where(o => o.EndDate == today && o.Status == Domain.Modules.Orders.OrderStatus.TəhvilVerilmiş),
            "active"         => mine.Where(o => o.Status == Domain.Modules.Orders.OrderStatus.TəhvilVerilmiş),
            "pending"        => mine.Where(o => o.Status is Domain.Modules.Orders.OrderStatus.Qaralama or Domain.Modules.Orders.OrderStatus.Təsdiqlənmiş),
            _                => mine
        };

        var list = mine
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.ToDto())
            .ToList();

        return Result.Success<IReadOnlyList<OrderDto>>(list);
    }
}
