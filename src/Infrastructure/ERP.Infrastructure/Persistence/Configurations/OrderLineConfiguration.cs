using ERP.Domain.Modules.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// OrderLine konfiqurasiyası. UnitPrice owned Money kimi map olunur; hesablanan LineTotal
/// map olunmur.
/// </summary>
public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Quantity).IsRequired();
        builder.HasIndex(l => l.ProductId);

        builder.OwnsOne(l => l.UnitPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(l => l.LineTotal);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
