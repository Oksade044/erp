using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.Services;

/// <summary>
/// ERP API üçün tipli HTTP client (TDD §31 — UI həmişə API ilə işləyir, birbaşa DB-yə yox).
/// Bütün çağırışlar Shared.Contracts DTO müqavilələrini istifadə edir.
/// </summary>
public sealed class ErpApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // --- Müştərilər ---
    public Task<PagedResult<CustomerDto>?> GetCustomersAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<CustomerDto>>(
            $"/api/v1/customers?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

    public async Task<(bool ok, string? error)> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/customers", request, JsonOpts, ct);
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

    // --- Sifarişlər ---
    public Task<PagedResult<OrderDto>?> GetOrdersAsync(string? search, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PagedResult<OrderDto>>(
            $"/api/v1/orders?search={Uri.EscapeDataString(search ?? "")}", JsonOpts, ct);

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
