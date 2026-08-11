using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Shared.Contracts.Auth;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Invoices;
using ERP.Shared.Contracts.Mobile;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Mobile.Services;

/// <summary>
/// ERP API müştərisi (mobil). Masaüstü ErpApiClient ilə eyni nümunə — REST /api/v1,
/// JWT Bearer. Bütün əməliyyatlar ERP ilə real vaxtda sinxrondur (ayrıca baza yoxdur).
/// </summary>
public sealed class MobileApiClient(AppState state)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private HttpClient Http()
    {
        var http = new HttpClient { BaseAddress = new Uri(state.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        if (!string.IsNullOrEmpty(state.AccessToken))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", state.AccessToken);
        return http;
    }

    // --- Auth ---
    public async Task<(bool ok, string? error)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            using var http = Http();
            var resp = await http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password), JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
                return (false, resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "İstifadəçi adı və ya şifrə yanlışdır."
                    : $"Xəta: {(int)resp.StatusCode}");
            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts, ct);
            if (auth is null) return (false, "Cavab oxunmadı.");
            state.AccessToken = auth.AccessToken;
            state.RefreshToken = auth.RefreshToken;
            state.User = auth;
            return (true, null);
        }
        catch (Exception ex) { return (false, $"Serverə qoşulmaq mümkün olmadı: {ex.Message}"); }
    }

    // --- Mən (mobil, işçiyə xas) ---
    public Task<EmployeeDashboardDto?> GetMyDashboardAsync(CancellationToken ct = default) =>
        GetAsync<EmployeeDashboardDto>("/api/v1/me/dashboard", ct);

    public Task<EmployeeFinanceDto?> GetMyFinanceAsync(CancellationToken ct = default) =>
        GetAsync<EmployeeFinanceDto>("/api/v1/me/finance", ct);

    public async Task<List<OrderDto>> GetMyOrdersAsync(string filter = "all", CancellationToken ct = default) =>
        await GetAsync<List<OrderDto>>($"/api/v1/me/orders?filter={filter}", ct) ?? [];

    // Mənim borcum (#17)
    public Task<ERP.Shared.Contracts.Representatives.RepresentativeLedgerDto?> GetMyDebtAsync(CancellationToken ct = default) =>
        GetAsync<ERP.Shared.Contracts.Representatives.RepresentativeLedgerDto>("/api/v1/me/debt", ct);

    // Mənim müştərilərim (təmsilçiyə təyin edilmiş) — sifariş yaratmada seçim üçün.
    public async Task<List<CustomerDto>> GetMyCustomersAsync(CancellationToken ct = default) =>
        await GetAsync<List<CustomerDto>>("/api/v1/me/customers", ct) ?? [];

    // --- Sifariş detalı + status ---
    public Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrderDto>($"/api/v1/orders/{id}", ct);

    public Task<(bool ok, string? error)> ConfirmOrderAsync(Guid id, CancellationToken ct = default) => PostActionAsync($"/api/v1/orders/{id}/confirm", ct);
    public Task<(bool ok, string? error)> DeliverOrderAsync(Guid id, CancellationToken ct = default) => PostActionAsync($"/api/v1/orders/{id}/deliver", ct);
    public Task<(bool ok, string? error)> ReturnOrderAsync(Guid id, CancellationToken ct = default) => PostActionAsync($"/api/v1/orders/{id}/return", ct);
    public Task<(bool ok, string? error)> CancelOrderAsync(Guid id, CancellationToken ct = default) => PostActionAsync($"/api/v1/orders/{id}/cancel", ct);

    public Task<(bool ok, string? error)> SetDepositAsync(Guid id, decimal deposit, CancellationToken ct = default) =>
        PostJsonAsync($"/api/v1/orders/{id}/deposit", new SetDepositRequest(deposit), ct);

    public Task<(bool ok, string? error)> SettleOrderAsync(Guid id, decimal damage, decimal penalty, string? notes, CancellationToken ct = default) =>
        PostJsonAsync($"/api/v1/orders/{id}/settle", new SettleOrderRequest(damage, penalty, notes), ct);

    // --- Fakturalar / ödənişlər ---
    public async Task<List<InvoiceDto>> GetInvoicesAsync(string? search = null, CancellationToken ct = default)
    {
        var paged = await GetAsync<PagedResult<InvoiceDto>>($"/api/v1/invoices?search={Uri.EscapeDataString(search ?? "")}&pageSize=200", ct);
        return paged?.Items?.ToList() ?? [];
    }

    public Task<(bool ok, string? error)> AddPaymentAsync(Guid invoiceId, decimal amount, string method, string? note, CancellationToken ct = default) =>
        PostJsonAsync($"/api/v1/invoices/{invoiceId}/payments", new AddPaymentRequest(amount, method, null, note), ct);

    public string InvoicePdfUrl(Guid invoiceId) => $"{state.BaseUrl}/api/v1/invoices/{invoiceId}/pdf";

    // --- Müştəri axtarışı / yaratma ---
    public async Task<List<CustomerDto>> SearchCustomersAsync(string? search, CancellationToken ct = default)
    {
        var paged = await GetAsync<PagedResult<CustomerDto>>($"/api/v1/customers?search={Uri.EscapeDataString(search ?? "")}&pageSize=200", ct);
        return paged?.Items?.ToList() ?? [];
    }

    public async Task<(Guid? id, string? error)> CreateCustomerAsync(CreateCustomerRequest req, CancellationToken ct = default)
    {
        try
        {
            using var http = Http();
            var resp = await http.PostAsJsonAsync("/api/v1/customers", req, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode) return (null, await ErrorAsync(resp, ct));
            var body = await resp.Content.ReadFromJsonAsync<IdResponse>(JsonOpts, ct);
            return (body?.Id, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // --- Məhsul axtarışı + anbar mövcudluğu ---
    public async Task<List<ProductDto>> SearchProductsAsync(string? search, CancellationToken ct = default)
    {
        var paged = await GetAsync<PagedResult<ProductDto>>($"/api/v1/products?search={Uri.EscapeDataString(search ?? "")}&pageSize=200", ct);
        return paged?.Items?.ToList() ?? [];
    }

    public async Task<List<StockLevelDto>> GetProductStockAsync(Guid productId, CancellationToken ct = default) =>
        await GetAsync<List<StockLevelDto>>($"/api/v1/products/{productId}/stock", ct) ?? [];

    public async Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken ct = default)
    {
        var paged = await GetAsync<PagedResult<WarehouseDto>>("/api/v1/warehouses?pageSize=100", ct);
        return paged?.Items?.ToList() ?? [];
    }

    // --- Sifariş yaratma ---
    public async Task<(Guid? id, string? error)> CreateOrderAsync(CreateOrderRequest req, CancellationToken ct = default)
    {
        try
        {
            using var http = Http();
            var resp = await http.PostAsJsonAsync("/api/v1/orders", req, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode) return (null, await ErrorAsync(resp, ct));
            var body = await resp.Content.ReadFromJsonAsync<IdResponse>(JsonOpts, ct);
            return (body?.Id, null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // --- ortaq köməkçilər ---
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var http = Http();
        return await http.GetFromJsonAsync<T>(path, JsonOpts, ct);
    }

    private async Task<(bool ok, string? error)> PostActionAsync(string path, CancellationToken ct)
    {
        try
        {
            using var http = Http();
            var resp = await http.PostAsync(path, null, ct);
            return resp.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(resp, ct));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private async Task<(bool ok, string? error)> PostJsonAsync<TBody>(string path, TBody body, CancellationToken ct)
    {
        try
        {
            using var http = Http();
            var resp = await http.PostAsJsonAsync(path, body, JsonOpts, ct);
            return resp.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(resp, ct));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private static async Task<string> ErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts, ct);
            return err?.Error ?? $"Xəta: {(int)resp.StatusCode}";
        }
        catch { return $"Xəta: {(int)resp.StatusCode}"; }
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
