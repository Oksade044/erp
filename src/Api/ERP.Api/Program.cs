using ERP.Api.Auth;
using ERP.Api.Endpoints;
using ERP.Api.Middleware;
using ERP.Application;
using ERP.Application.Common.Interfaces;
using ERP.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
