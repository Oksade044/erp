using ERP.Domain.Modules.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>Payment konfiqurasiyası. Amount owned Money kimi map olunur.</summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaidAt).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(500);

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
