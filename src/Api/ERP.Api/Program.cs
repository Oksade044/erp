using ERP.Api.Auth;
using ERP.Api.Endpoints;
using ERP.Api.Middleware;
using ERP.Application;
using ERP.Application.Common.Interfaces;
using ERP.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog struktur logging (TDD §19) ---
// Lokalda console + rolling fayl sink; serverdə eyni konfiqurasiya konsola (Docker log) yazır.
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

// OpenAPI/Swagger (TDD §11)
builder.Services.AddOpenApi();

var app = builder.Build();

// Global exception handling (TDD §21) — pipeline-ın əvvəlində.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Hər HTTP sorğusunu struktur şəkildə loglayır (metod, yol, status, müddət) — TDD §19.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // İnteraktiv API interfeysi (canlı test üçün): /scalar
    app.MapScalarApiReference(options =>
        options.WithTitle("ERP API — Toy Dekoru & Tədbir Avadanlığı"));
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ERP.Api" }))
   .WithName("HealthCheck");

app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();
