using ERP.Domain.Modules.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// PurchaseOrder aggregate konfiqurasiyası. Sətirlər ayrı cədvəldə, aggregate-ə FK ilə bağlı;
/// hesablanan Total map olunmur. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PurchaseNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.PurchaseNumber).IsUnique();

        builder.Property(p => p.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.OrderDate).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.SupplierId);

        builder.HasMany(p => p.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.Total);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
