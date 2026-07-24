using System.Text;
using System.Threading.RateLimiting;
using ERP.Api.Auth;
using ERP.Api.Endpoints;
using ERP.Api.Middleware;
using ERP.Application;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Users;
using ERP.Infrastructure;
using ERP.Infrastructure.Auth;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog struktur logging (TDD §19) ---
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/erp-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// --- Layer DI qeydiyyatı (TDD §18) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Cari istifadəçi konteksti (TDD §7, §20)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// --- Autentifikasiya: JWT Bearer (TDD §6) ---
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

// --- Avtorizasiya: default olaraq bütün endpoint-lər autentifikasiya tələb edir (TDD §7, §39) ---
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    // İcazə-əsaslı siyasətlər (permission-based RBAC).
    options.AddPolicy(Permissions.OrdersApprove, p => p.RequireClaim("permission", Permissions.OrdersApprove));
    options.AddPolicy(Permissions.ReportsView, p => p.RequireClaim("permission", Permissions.ReportsView));
    options.AddPolicy(Permissions.InvoicesEdit, p => p.RequireClaim("permission", Permissions.InvoicesEdit));
    options.AddPolicy(Permissions.UsersManage, p => p.RequireClaim("permission", Permissions.UsersManage));
    options.AddPolicy(Permissions.ProductsEdit, p => p.RequireClaim("permission", Permissions.ProductsEdit));
    options.AddPolicy(Permissions.SuppliersView, p => p.RequireClaim("permission", Permissions.SuppliersView));
    options.AddPolicy(Permissions.SuppliersEdit, p => p.RequireClaim("permission", Permissions.SuppliersEdit));
    options.AddPolicy(Permissions.PurchasesView, p => p.RequireClaim("permission", Permissions.PurchasesView));
    options.AddPolicy(Permissions.PurchasesEdit, p => p.RequireClaim("permission", Permissions.PurchasesEdit));
    options.AddPolicy(Permissions.FinanceView, p => p.RequireClaim("permission", Permissions.FinanceView));
    options.AddPolicy(Permissions.FinanceEdit, p => p.RequireClaim("permission", Permissions.FinanceEdit));
    options.AddPolicy(Permissions.HrView, p => p.RequireClaim("permission", Permissions.HrView));
    options.AddPolicy(Permissions.HrEdit, p => p.RequireClaim("permission", Permissions.HrEdit));
    options.AddPolicy(Permissions.WarehousesView, p => p.RequireClaim("permission", Permissions.WarehousesView));
    options.AddPolicy(Permissions.WarehousesEdit, p => p.RequireClaim("permission", Permissions.WarehousesEdit));
});

// --- Rate limiting (TDD §39 — sui-istifadəyə qarşı müdafiə) ---
// Qlobal: hər IP üçün sabit pəncərə. "auth": login/refresh üçün daha sərt (brute-force qorunması).
// Limitlər appsettings-dən (RateLimiting) — kodda magic number yoxdur.
var rl = builder.Configuration.GetSection("RateLimiting");
var globalPermit = rl.GetValue("GlobalPermitLimit", 100);
var globalWindow = rl.GetValue("GlobalWindowSeconds", 10);
var authPermit = rl.GetValue("AuthPermitLimit", 5);
var authWindow = rl.GetValue("AuthWindowSeconds", 30);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Qlobal limiter — bütün sorğulara, klient IP-si üzrə bölmələnmiş.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = globalPermit,
            Window = TimeSpan.FromSeconds(globalWindow),
            QueueLimit = 0
        });
    });

    // "auth" siyasəti — login/refresh üçün sərt limit (IP üzrə).
    options.AddPolicy("auth", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"auth:{key}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermit,
            Window = TimeSpan.FromSeconds(authWindow),
            QueueLimit = 0
        });
    });

    // Rədd edildikdə mənalı JSON cavab (ProblemDetails üslubunda, TDD §21).
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Həddindən çox sorğu. Bir azdan yenidən cəhd edin.",
            status = 429
        }, ct);
    };
});

// --- Background jobs: Hangfire (TDD §36) ---
// Lokalda in-memory storage; serverdə PostgreSQL storage-a keçiriləcək.
builder.Services.AddHangfire(config => config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// --- Real-time: SignalR canlı stok bildirişi (TDD §38) ---
builder.Services.AddSignalR();
builder.Services.AddScoped<IStockNotifier, ERP.Api.Realtime.SignalRStockNotifier>();

// OpenAPI/Swagger (TDD §11)
builder.Services.AddOpenApi();

var app = builder.Build();

// Global exception handling (TDD §21) — pipeline-ın əvvəlində.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

// Rate limiting — autentifikasiyadan əvvəl (sui-istifadəni erkən dayandırır, TDD §39).
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options =>
        options.WithTitle("ERP API — Toy Dekoru & Tədbir Avadanlığı")).AllowAnonymous();
    // Hangfire dashboard yalnız lokalda (default: yalnız localhost girişi) — /hangfire.
    app.UseHangfireDashboard("/hangfire");
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ERP.Api" }))
   .WithName("HealthCheck").AllowAnonymous();

// Canlı stok hub-ı (TDD §38). Lokalda anonim (API-first, lokal PC).
app.MapHub<ERP.Api.Realtime.StockHub>("/hubs/stock").AllowAnonymous();

app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapInvoiceEndpoints();
app.MapSupplierEndpoints();
app.MapPurchaseEndpoints();
app.MapFinanceEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();
app.MapPayrollEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapReportEndpoints();
app.MapUserEndpoints();

// Backup: manual trigger (users.manage) — Hangfire background job kimi növbəyə salır (TDD §29, §36).
app.MapPost("/api/v1/admin/backup", (IBackgroundJobClient jobs) =>
{
    var jobId = jobs.Enqueue<IBackupService>(s => s.BackupAsync(CancellationToken.None));
    return Results.Accepted(value: new { jobId, message = "Backup növbəyə salındı." });
}).RequireAuthorization(Permissions.UsersManage).WithTags("Admin");

// Gündəlik avtomatik backup (TDD §29).
RecurringJob.AddOrUpdate<IBackupService>(
    "daily-backup", s => s.BackupAsync(CancellationToken.None), Cron.Daily);

// İlkin data: admin istifadəçisi + gözləyən migration-lar (TDD §6).
await DbSeeder.SeedAsync(app.Services);

app.Run();
