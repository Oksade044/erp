using ERP.Domain.Modules.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// RentalOrder aggregate konfiqurasiyası. Sətirlər ayrı cədvəldə, aggregate-ə FK ilə bağlı;
/// hesablanan Total/ReservesStock map olunmur. Provider-a xas SQL yoxdur (TDD §4, §14).
/// </summary>
public sealed class RentalOrderConfiguration : IEntityTypeConfiguration<RentalOrder>
{
    public void Configure(EntityTypeBuilder<RentalOrder> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.StartDate).IsRequired();
        builder.Property(o => o.EndDate).IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.StartDate, o.EndDate });

        // Sətir kolleksiyası — private backing field (_lines) üzərindən.
        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(o => o.CreatedByName).HasMaxLength(200);
        builder.Property(o => o.CreatedByRole).HasMaxLength(50);
        builder.Property(o => o.ConfirmedByName).HasMaxLength(200);

        builder.Property(o => o.SettlementNotes).HasMaxLength(2000);
        builder.Property(o => o.IsSettled).IsRequired();

        // Depozit & hesablaşma məbləğləri — opsional owned Money (mövcud sətirlər üçün null).
        builder.OwnsOne(o => o.Deposit, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Deposit").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("DepositCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(o => o.DamageCharge, m =>
        {
            m.Property(x => x.Amount).HasColumnName("DamageCharge").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("DamageCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(o => o.PenaltyCharge, m =>
        {
            m.Property(x => x.Amount).HasColumnName("PenaltyCharge").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("PenaltyCurrency").HasMaxLength(3);
        });

        // Hesablanan xüsusiyyətlər DB-yə map olunmur.
        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.ReservesStock);
        builder.Ignore(o => o.TotalCharges);
        builder.Ignore(o => o.DepositRefund);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
