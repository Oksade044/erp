using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Products;

/// <summary>
/// Məhsul (icarə avadanlığı) — aggregate root, rich domain model (TDD §13).
/// İcarə qiyməti Money ilə modelləşir (sifariş səviyyəsində dinamik dəyişə bilər).
/// Anbar izləmə rejimi biznes invariantı ilə qorunur.
/// </summary>
public class Product : BaseEntity, IAggregateRoot
{
    public Sku Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Category { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Bir icarə üçün baza qiyməti (sifarişdə dinamik dəyişə bilər — TDD prinsip §7).</summary>
    public Money RentalPrice { get; private set; } = null!;

    /// <summary>Avadanlığın alış (maya) qiyməti — opsional, həssas (yalnız Admin/Menecer görür).</summary>
    public Money? PurchasePrice { get; private set; }

    /// <summary>Satış qiyməti — avadanlıq satılarsa, opsional.</summary>
    public Money? SalePrice { get; private set; }

    public ProductTrackingMode TrackingMode { get; private set; }

    /// <summary>Mövcud anbar sayı. Toplu rejimdə birbaşa idarə olunur.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>Minimum stok həddi — bundan aşağı düşəndə "az qalıb" xəbərdarlığı verilir.</summary>
    public int MinStockQuantity { get; private set; }

    /// <summary>Anbar sayı minimum həddə çatıb və ya aşağıdırsa (hədd təyin olunubsa).</summary>
    public bool IsLowStock => MinStockQuantity > 0 && StockQuantity <= MinStockQuantity;

    public bool IsActive { get; private set; } = true;

    /// <summary>Məhsul şəklinin saxlama açarı/yolu (fayl storage-də; DB-də yalnız yol — TDD §24).</summary>
    public string? ImagePath { get; private set; }

    /// <summary>Ölçü vahidi (#12) — Ədəd, Kg, Litr, Metr, Dəst və s. Default: Ədəd.</summary>
    public string Unit { get; private set; } = "Ədəd";

    // EF Core üçün.
    private Product() { }

    private Product(Sku sku, string name, Money rentalPrice, ProductTrackingMode trackingMode)
    {
        Sku = sku;
        Name = name;
        RentalPrice = rentalPrice;
        TrackingMode = trackingMode;
    }

    public static Product Create(
        Sku sku,
        string name,
        Money rentalPrice,
        ProductTrackingMode trackingMode,
        int stockQuantity = 0,
        string? category = null,
        string? description = null,
        Money? purchasePrice = null,
        Money? salePrice = null,
        int minStockQuantity = 0,
        string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Məhsul adı tələb olunur.");
        if (stockQuantity < 0)
            throw new DomainException("Anbar sayı mənfi ola bilməz.");
        if (minStockQuantity < 0)
            throw new DomainException("Minimum stok mənfi ola bilməz.");

        return new Product(sku, name.Trim(), rentalPrice, trackingMode)
        {
            StockQuantity = stockQuantity,
            Category = category?.Trim(),
            Description = description?.Trim(),
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            MinStockQuantity = minStockQuantity,
            Unit = string.IsNullOrWhiteSpace(unit) ? "Ədəd" : unit.Trim()
        };
    }

    public void ChangeUnit(string? unit) => Unit = string.IsNullOrWhiteSpace(unit) ? "Ədəd" : unit.Trim();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Məhsul adı tələb olunur.");
        Name = name.Trim();
    }

    public void ChangeCategory(string? category) => Category = category?.Trim();
    public void ChangeDescription(string? description) => Description = description?.Trim();

    public void ChangePrice(Money price) =>
        RentalPrice = price ?? throw new DomainException("İcarə qiyməti tələb olunur.");

    /// <summary>Alış qiymətini təyin edir (null = silinir).</summary>
    public void ChangePurchasePrice(Money? price) => PurchasePrice = price;

    /// <summary>Satış qiymətini təyin edir (null = silinir).</summary>
    public void ChangeSalePrice(Money? price) => SalePrice = price;

    public void SetMinStock(int minStock)
    {
        if (minStock < 0)
            throw new DomainException("Minimum stok mənfi ola bilməz.");
        MinStockQuantity = minStock;
    }

    public void ChangeTrackingMode(ProductTrackingMode mode)
    {
        // Invariant: rejim dəyişikliyi anbar bütövlüyünü pozmamalıdır.
        // Nüsxə izləməyə keçid gələcəkdə mövcud nüsxələrin qeydiyyatını tələb edəcək
        // (ProductUnit entity əlavə olunanda genişlənəcək).
        TrackingMode = mode;
    }

    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Anbar sayı mənfi ola bilməz.");
        StockQuantity = quantity;
    }

    public void AdjustStock(int delta)
    {
        var updated = StockQuantity + delta;
        if (updated < 0)
            throw new DomainException("Anbar sayı mənfi ola bilməz.");
        StockQuantity = updated;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>Şəkil yolunu təyin edir (fayl storage açarı). Silmək üçün null verilir.</summary>
    public void SetImagePath(string? path) => ImagePath = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}
