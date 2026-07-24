using ERP.Domain.Modules.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// StockLevel konfiqurasiyası. Bir məhsul + anbar cütü üçün bir qeyd (unikal indeks).
/// Hesablanan IsLow map olunmur. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.WarehouseName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Quantity).IsRequired();
        builder.Property(s => s.MinQuantity).IsRequired();

        builder.Ignore(s => s.IsLow);

        builder.HasIndex(s => new { s.ProductId, s.WarehouseId }).IsUnique();
        builder.HasIndex(s => s.WarehouseId);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
