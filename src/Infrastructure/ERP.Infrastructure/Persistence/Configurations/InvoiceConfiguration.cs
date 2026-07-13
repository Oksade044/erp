using ERP.Domain.Modules.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Invoice aggregate konfiqurasiyası. Ödənişlər ayrı cədvəldə; hesablanan sahələr (AmountPaid,
/// Balance, Status) map olunmur. Provider-a xas SQL yoxdur (TDD §4, §14).
/// </summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.IssueDate).IsRequired();

        builder.HasIndex(i => i.OrderId).IsUnique();   // bir sifariş üçün bir faktura
        builder.HasIndex(i => i.CustomerId);

        builder.OwnsOne(i => i.TotalAmount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(i => i.AmountPaid);
        builder.Ignore(i => i.Balance);
        builder.Ignore(i => i.Status);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
