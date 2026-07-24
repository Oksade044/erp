using ERP.Domain.Modules.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Warehouse konfiqurasiyası. Address/Phone owned VO; kod unikal. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique();

        builder.Property(w => w.Notes).HasMaxLength(2000);
        builder.Property(w => w.IsActive).IsRequired();

        builder.OwnsOne(w => w.Phone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("Phone").HasMaxLength(20);
        });

        builder.OwnsOne(w => w.Address, addr =>
        {
            addr.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            addr.Property(a => a.Line).HasColumnName("AddressLine").HasMaxLength(500);
        });

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
