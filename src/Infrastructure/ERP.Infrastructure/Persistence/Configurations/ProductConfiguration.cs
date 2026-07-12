using ERP.Domain.Modules.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product EF Core konfiqurasiyası. Sku və RentalPrice (Money) owned type kimi map olunur;
/// provider-a xas SQL yoxdur (TDD §4, §14).
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();

        builder.Property(p => p.TrackingMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // SKU — owned VO, unikal.
        builder.OwnsOne(p => p.Sku, sku =>
        {
            sku.Property(s => s.Value).HasColumnName("Sku").HasMaxLength(40).IsRequired();
            sku.HasIndex(s => s.Value).IsUnique();
        });

        // İcarə qiyməti — owned Money VO.
        builder.OwnsOne(p => p.RentalPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("RentalPrice").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        // Soft delete (TDD §13).
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
