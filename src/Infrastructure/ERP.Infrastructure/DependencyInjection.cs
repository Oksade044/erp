using ERP.Application.Common.Interfaces;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Persistence.Interceptors;
using ERP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Infrastructure;

/// <summary>
/// Infrastructure layer-in DI qeydiyyatı (TDD §18). Provider seçimi burada, kodda deyil —
/// SQLite→PostgreSQL keçidi konfiqurasiya ilə (TDD §4, §32).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ?? "Data Source=erp.db";

        // Audit interceptor (TDD §20) — DbContext-ə qoşulur.
        services.AddScoped<AuditInterceptor>();

        // Provider hələlik SQLite (lokal). Serverdə UseNpgsql-a keçid yalnız bu sətir + connection string.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // AppDbContext eyni zamanda IUnitOfWork-dur (TDD §15).
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Generic + domenə xas repository-lər (TDD §14).
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRentalOrderRepository, RentalOrderRepository>();

        return services;
    }
}
