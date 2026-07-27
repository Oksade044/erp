using ERP.Domain.Modules.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// SupplierLedgerEntry konfiqurasiyası (#15). Amount owned Money; hesablanan SignedAmount
/// map olunmur. SupplierId üzrə indeks (defter oxumaları üçün). Provider-agnostik.
/// </summary>
public sealed class SupplierLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierLedgerEntry> builder)
    {
        builder.ToTable("SupplierLedgerEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SupplierId).IsRequired();
        builder.Property(e => e.Date).IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.DocumentPath).HasMaxLength(400);

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(e => e.SignedAmount);
        builder.HasIndex(e => e.SupplierId);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
