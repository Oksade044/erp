using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Mobile;

namespace ERP.Api.Endpoints;

/// <summary>
/// Mobil işçi tətbiqi üçün "mən"ə xas endpoint-lər (#mobil). Bütün nəticələr cari
/// autentifikasiya olunmuş istifadəçiyə görə süzülür — işçi yalnız öz işini görür.
/// Ayrıca icazə tələb olunmur (login kifayətdir); süzgəc identifikasiyaya bağlıdır.
/// </summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me").WithTags("Me (Mobile)");

        group.MapGet("/dashboard", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyDashboardQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/finance", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyFinanceQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Mənim borcum (#17) — cari təmsilçi balansı + qeydlər.
        group.MapGet("/debt", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyDebtQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Mənim müştərilərim (təmsilçiyə təyin edilmiş) — mobil sifariş yaratmada seçim üçün.
        group.MapGet("/customers", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMyCustomersQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // filter: all | today-delivery | today-return | active | pending
        group.MapGet("/orders", async (ISender sender, string? filter) =>
        {
            var result = await sender.Send(new GetMyOrdersQuery(filter ?? "all"));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
