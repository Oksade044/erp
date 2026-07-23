using ERP.Domain.Modules.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// FinancialTransaction konfiqurasiyası. Amount owned Money kimi map olunur; hesablanan
/// SignedAmount map olunmur. Provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("FinancialTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(t => t.TransactionNumber).IsUnique();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Category).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.Property(t => t.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(t => t.SignedAmount);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.Date);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
