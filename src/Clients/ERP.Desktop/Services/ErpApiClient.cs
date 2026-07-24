using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using ERP.Shared.Contracts.Auth;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Finance;
using ERP.Shared.Contracts.Hr;
using ERP.Shared.Contracts.Invoices;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Purchases;
using ERP.Shared.Contracts.Reports;
using ERP.Shared.Contracts.Suppliers;
using ERP.Shared.Contracts.Warehouses;
using ERP.Shared.Contracts.Users;

namespace ERP.Desktop.Services;

/// <summary>
/// ERP API üçün tipli HTTP client (TDD §31 — UI həmişə API ilə işləyir, birbaşa DB-yə yox).
/// Bütün çağırışlar Shared.Contracts DTO müqavilələrini istifadə edir.
/// </summary>
public sealed class ErpApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>API-nin baza ünvanı (SignalR hub URL-i qurmaq üçün).</summary>
    public string BaseUrl => http.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5080";

    // --- Autentifikasiya ---
    public async Task<(AuthResponse? auth, string? error)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password), JsonOpts, ct);
            if (resp.IsSuccessStatusCode)
            {
                var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts, ct);
                return (auth, null);
            }
            return (null, "İstifadəçi adı və ya parol yanlışdır.");
        }
        catch (Exception ex)
        {
            return (null, $"Serverə qoşulmaq mümkün olmadı: {ex.Message}");
        }
    }

    /// <summary>Bearer token-i bütün sonrakı sorğulara qoşur.</summary>
    public void SetBearerToken(string? token) =>
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);

    // --- Müştərilər ---
    public Task<PagedResult<CustomerDto>?> GetCustomersAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<CustomerDto>>(
            $"/api/v1/customers?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/customers", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Təchizatçılar ---
    public Task<PagedResult<SupplierDto>?> GetSuppliersAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<SupplierDto>>(
            $"/api/v1/suppliers?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/suppliers", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Məhsullar ---
    public Task<PagedResult<ProductDto>?> GetProductsAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/v1/products?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/products", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<byte[]?> ExportProductsExcelAsync(CancellationToken ct = default)
    {
        var resp = await http.GetAsync("/api/v1/products/export", ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync(ct) : null;
    }

    public async Task<byte[]?> GetProductQrAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/api/v1/products/{id}/qr", ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync(ct) : null;
    }

    // --- Sifarişlər ---
    public Task<PagedResult<OrderDto>?> GetOrdersAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<OrderDto>>(
            $"/api/v1/orders?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/orders", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> ConfirmOrderAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/orders/{id}/confirm", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> CancelOrderAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/orders/{id}/cancel", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> DeliverOrderAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/orders/{id}/deliver", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> ReturnOrderAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/orders/{id}/return", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> SetOrderDepositAsync(Guid id, decimal deposit, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"/api/v1/orders/{id}/deposit", new SetDepositRequest(deposit), JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> SettleOrderAsync(Guid id, decimal damage, decimal penalty, string? notes, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"/api/v1/orders/{id}/settle",
            new SettleOrderRequest(damage, penalty, notes), JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Alışlar (Purchase) ---
    public Task<PagedResult<PurchaseDto>?> GetPurchasesAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<PurchaseDto>>(
            $"/api/v1/purchases?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreatePurchaseAsync(CreatePurchaseRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/purchases", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> ConfirmPurchaseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/purchases/{id}/confirm", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> ReceivePurchaseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/purchases/{id}/receive", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> CancelPurchaseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/purchases/{id}/cancel", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- İşçilər (HR) ---
    public Task<PagedResult<EmployeeDto>?> GetEmployeesAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<EmployeeDto>>(
            $"/api/v1/employees?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/employees", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Anbarlar (Warehouse) ---
    public Task<PagedResult<WarehouseDto>?> GetWarehousesAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<WarehouseDto>>(
            $"/api/v1/warehouses?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/warehouses", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Stok (per-anbar səviyyələr, transfer, min-stok) ---
    public Task<PagedResult<StockLevelDto>?> GetStockLevelsAsync(string? search, bool lowOnly, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/v1/stock/levels?search={Uri.EscapeDataString(search ?? "")}&low={(lowOnly ? "true" : "false")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/stock/adjust", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> TransferStockAsync(TransferStockRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/stock/transfer", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Əməkhaqqı (Payroll) ---
    public Task<PagedResult<PayrollDto>?> GetPayrollsAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<PayrollDto>>(
            $"/api/v1/payrolls?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreatePayrollAsync(CreatePayrollRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/payrolls", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> PayPayrollAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"/api/v1/payrolls/{id}/pay", null, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Davamiyyət (Attendance) ---
    public Task<PagedResult<AttendanceDto>?> GetAttendanceAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<AttendanceDto>>(
            $"/api/v1/attendance?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateAttendanceAsync(CreateAttendanceRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/attendance", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Maliyyə (kassa mədaxil/məxaric) ---
    public Task<PagedResult<TransactionDto>?> GetTransactionsAsync(string? search, string? type, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<TransactionDto>>(
            $"/api/v1/finance/transactions?search={Uri.EscapeDataString(search ?? "")}&type={Uri.EscapeDataString(type ?? "")}", JsonOpts, ct);

    public Task<CashFlowSummaryDto?> GetCashFlowSummaryAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<CashFlowSummaryDto>("/api/v1/finance/summary", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/finance/transactions", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    // --- Fakturalar ---
    public Task<PagedResult<InvoiceDto>?> GetInvoicesAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<InvoiceDto>>(
            $"/api/v1/invoices?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateInvoiceAsync(Guid orderId, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest(orderId), JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<(bool ok, string? error)> AddInvoicePaymentAsync(
        Guid invoiceId, AddPaymentRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    public async Task<byte[]?> GetInvoicePdfAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/api/v1/invoices/{invoiceId}/pdf", ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync(ct) : null;
    }

    // --- Hesabatlar ---
    public Task<DashboardDto?> GetDashboardAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<DashboardDto>("/api/v1/reports/dashboard", JsonOpts, ct);

    public Task<List<OutstandingInvoiceDto>?> GetOutstandingAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<OutstandingInvoiceDto>>("/api/v1/reports/outstanding", JsonOpts, ct);

    public Task<List<TopProductDto>?> GetTopProductsAsync(int top = 10, CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<TopProductDto>>($"/api/v1/reports/top-products?top={top}", JsonOpts, ct);

    // --- İstifadəçilər ---
    public Task<List<UserDto>?> GetUsersAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<UserDto>>("/api/v1/users", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/users", request, JsonOpts, ct);
        return await ReadResultAsync(resp, ct);
    }

    private static async Task<(bool ok, string? error)> ReadResultAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode)
            return (true, null);

        // Xəta cavabından mənalı mesaj çıxar (Result error və ya ProblemDetails).
        try
        {
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (doc.TryGetProperty("error", out var err)) return (false, err.GetString());
            if (doc.TryGetProperty("detail", out var det)) return (false, det.GetString());
        }
        catch { /* struktursuz cavab */ }

        return (false, $"Xəta: {(int)resp.StatusCode} {resp.ReasonPhrase}");
    }
}

/// <summary>
/// Server-side pagination nəticəsi (API-nin PagedResult zərfinə uyğun). Desktop-a xas kopya
/// (Shared.Contracts DTO-larını istifadə edir, amma zərf Application layer-dədir).
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
