using System;
using ERP.Application.Common.Interfaces;
using ERP.Infrastructure.Auth;
using ERP.Infrastructure.Excel;
using ERP.Infrastructure.Pdf;
using ERP.Infrastructure.Reports;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Persistence.Interceptors;
using ERP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

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

        // Provider config ilə seçilir — app kodu dəyişmir (TDD §4, §32).
        // "Sqlite" (lokal, default) və ya "Postgres" (server). Yalnız bu registrasiya fərqlənir.
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        // Audit interceptor (TDD §20) — DbContext-ə qoşulur.
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                || provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString);
            else
                options.UseSqlite(connectionString);

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        // AppDbContext eyni zamanda IUnitOfWork-dur (TDD §15).
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Generic + domenə xas repository-lər (TDD §14).
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRentalOrderRepository, RentalOrderRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockLevelRepository, StockLevelRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IFieldPermissionRepository, FieldPermissionRepository>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();

        // Autentifikasiya servisləri (TDD §6, §39).
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        // PDF generasiya (TDD §25). QuestPDF Community lisenziyası (kiçik şirkətlər üçün pulsuz).
        QuestPDF.Settings.License = LicenseType.Community;
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();

        // Hesabat/analitika oxumaları (TDD §5).
        services.AddScoped<IReportService, ReportService>();

        // Excel idxal/ixrac (TDD §26).
        services.AddScoped<IExcelService, ExcelService>();

        // QR/barkod generasiyası (TDD §27).
        services.AddScoped<IBarcodeService, Barcodes.BarcodeService>();

        // Verilənlər bazası backup-ı (TDD §29).
        services.AddScoped<IBackupService, Backup.SqliteBackupService>();

        // Fayl saxlama (TDD §23) — lokalda disk; serverdə MinIO/S3-ə keçirilə bilər.
        services.AddScoped<IFileStorage, Files.LocalFileStorage>();

        // Keş (TDD §37) — lokalda IMemoryCache (default), serverdə Redis. Keçid yalnız konfiqurasiya.
        services.AddMemoryCache();
        var cacheProvider = configuration["Cache:Provider"] ?? "Memory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(o =>
                o.Configuration = configuration["Cache:RedisConnection"] ?? "localhost:6379");
            services.AddScoped<ICacheService, Caching.RedisCacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, Caching.MemoryCacheService>();
        }

        return services;
    }
}
