using ERP.Domain.Modules.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// PurchaseLine konfiqurasiyası. UnitCost owned Money kimi map olunur; hesablanan LineTotal
/// map olunmur.
/// </summary>
public sealed class PurchaseLineConfiguration : IEntityTypeConfiguration<PurchaseLine>
{
    public void Configure(EntityTypeBuilder<PurchaseLine> builder)
    {
        builder.ToTable("PurchaseLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Quantity).IsRequired();
        builder.HasIndex(l => l.ProductId);

        builder.OwnsOne(l => l.UnitCost, cost =>
        {
            cost.Property(m => m.Amount).HasColumnName("UnitCost").HasPrecision(18, 2).IsRequired();
            cost.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(l => l.LineTotal);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
