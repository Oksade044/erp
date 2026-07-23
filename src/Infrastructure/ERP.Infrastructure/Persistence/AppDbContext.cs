using System.Reflection;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.Modules.Orders;
using ERP.Domain.Modules.Products;
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
    public DbSet<RentalOrder> Orders => Set<RentalOrder>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<User> Users => Set<User>();

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
