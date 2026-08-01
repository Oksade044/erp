using System.Reflection;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Hr;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.Modules.Orders;
using ERP.Domain.Modules.Products;
using ERP.Domain.Modules.Purchases;
using ERP.Domain.Modules.Warehouses;
using ERP.Domain.Modules.Suppliers;
using ERP.Domain.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — provider-agnostik (TDD §4). Provider (SQLite/PostgreSQL)
/// yalnız DI konfiqurasiyasında seçilir; burada provider-a xas heç nə yoxdur.
/// IUnitOfWork-u da bu context təmin edir (TDD §15).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ERP.Domain.Modules.Products.Category> Categories => Set<ERP.Domain.Modules.Products.Category>();
    public DbSet<RentalOrder> Orders => Set<RentalOrder>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierLedgerEntry> SupplierLedgerEntries => Set<SupplierLedgerEntry>();
    public DbSet<ERP.Domain.Modules.Representatives.RepresentativeEntry> RepresentativeEntries => Set<ERP.Domain.Modules.Representatives.RepresentativeEntry>();
    public DbSet<PurchaseOrder> Purchases => Set<PurchaseOrder>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ERP.Domain.Modules.Users.AppRole> Roles => Set<ERP.Domain.Modules.Users.AppRole>();
    public DbSet<ERP.Domain.Modules.Settings.FieldPermission> FieldPermissions => Set<ERP.Domain.Modules.Settings.FieldPermission>();
    public DbSet<ERP.Domain.Modules.Audit.AuditLog> AuditLogs => Set<ERP.Domain.Modules.Audit.AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bütün IEntityTypeConfiguration konfiqurasiyalarını bu assembly-dən tətbiq et.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // RowVersion (optimistic concurrency, TDD §35) — bütün BaseEntity-lər üçün provider-aware:
        //  • SQLite (lokal): native rowversion yoxdur → sadə konkurentlik sütunu, dəyəri app idarə edir.
        //  • SQL Server / PostgreSQL (server): əsl store-generated rowversion.
        var isSqlite = Database.IsSqlite();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var rowVersion = modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsConcurrencyToken();

            if (isSqlite)
                rowVersion.ValueGeneratedNever();
            else
                rowVersion.IsRowVersion();
        }

        base.OnModelCreating(modelBuilder);
    }

    // IUnitOfWork.SaveChangesAsync(CancellationToken) DbContext-in mövcud metodu ilə
    // avtomatik təmin olunur — əlavə implementasiya lazım deyil.
}
