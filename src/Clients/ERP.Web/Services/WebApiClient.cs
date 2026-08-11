using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Shared.Contracts.Auth;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Mobile;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Representatives;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Web.Services;

/// <summary>
/// ERP API müştərisi (web PWA). Native mobil MobileApiClient ilə eyni məntiq — REST /api/v1,
/// JWT Bearer. API eyni domendə Caddy vasitəsilə verilir → nisbi URL-lər (CORS yoxdur).
/// </summary>
public sealed class WebApiClient(HttpClient http, AppState state)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private void ApplyToken()
    {
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(state.AccessToken) ? null : new AuthenticationHeaderValue("Bearer", state.AccessToken);
    }

    // --- Auth ---
    public async Task<(bool ok, string? error)> LoginAsync(string username, string password)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password), JsonOpts);
            if (!resp.IsSuccessStatusCode)
                return (false, resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "İstifadəçi adı və ya şifrə yanlışdır." : $"Xəta: {(int)resp.StatusCode}");
            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
            if (auth is null) return (false, "Cavab oxunmadı.");
            await state.SetAuthAsync(auth);
            ApplyToken();
            return (true, null);
        }
        catch (Exception ex) { return (false, $"Serverə qoşulmaq mümkün olmadı: {ex.Message}"); }
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        ApplyToken();
        try { return await http.GetFromJsonAsync<T>(path, JsonOpts); }
        catch { return default; }
    }

    // --- Mən (işçi/təmsilçi) ---
    public Task<EmployeeDashboardDto?> GetMyDashboardAsync() => GetAsync<EmployeeDashboardDto>("/api/v1/me/dashboard");
    public Task<RepresentativeLedgerDto?> GetMyDebtAsync() => GetAsync<RepresentativeLedgerDto>("/api/v1/me/debt");
    public async Task<List<CustomerDto>> GetMyCustomersAsync() => await GetAsync<List<CustomerDto>>("/api/v1/me/customers") ?? [];

    // --- Məhsul + anbar ---
    public async Task<List<ProductDto>> SearchProductsAsync(string? search)
    {
        var paged = await GetAsync<PagedResult<ProductDto>>($"/api/v1/products?search={Uri.EscapeDataString(search ?? "")}&pageSize=200");
        return paged?.Items ?? [];
    }
    public async Task<List<StockLevelDto>> GetProductStockAsync(Guid productId) =>
        await GetAsync<List<StockLevelDto>>($"/api/v1/products/{productId}/stock") ?? [];

    // --- Sifariş yaratma ---
    public async Task<(Guid? id, string? error)> CreateOrderAsync(CreateOrderRequest req)
    {
        ApplyToken();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/v1/orders", req, JsonOpts);
            if (!resp.IsSuccessStatusCode)
            {
                try { var e = await resp.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts); return (null, e?.Error ?? $"Xəta: {(int)resp.StatusCode}"); }
                catch { return (null, $"Xəta: {(int)resp.StatusCode}"); }
            }
            var body = await resp.Content.ReadFromJsonAsync<IdResponse>(JsonOpts);
            return (body?.Id, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    private sealed record IdResponse(Guid Id);
    private sealed record ErrorResponse(string Error);
}

/// <summary>Səhifələnmiş nəticə (API ilə eyni forma).</summary>
public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
