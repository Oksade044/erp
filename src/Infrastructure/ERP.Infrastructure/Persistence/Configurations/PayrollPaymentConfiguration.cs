using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// PayrollPayment (əməkhaqqı üzrə hissə-hissə ödəniş) konfiqurasiyası. Amount owned Money.
/// Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class PayrollPaymentConfiguration : IEntityTypeConfiguration<PayrollPayment>
{
    public void Configure(EntityTypeBuilder<PayrollPayment> builder)
    {
        builder.ToTable("PayrollPayments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Date).IsRequired();
        builder.Property(p => p.Method).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.Property(p => p.IsBonus).IsRequired();

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
