using ERP.Domain.Modules.Representatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>RepresentativeEntry konfiqurasiyası (#16-18). Amount owned Money; hesablanan SignedAmount map olunmur.</summary>
public sealed class RepresentativeEntryConfiguration : IEntityTypeConfiguration<RepresentativeEntry>
{
    public void Configure(EntityTypeBuilder<RepresentativeEntry> builder)
    {
        builder.ToTable("RepresentativeEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RepresentativeName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.OrderNumber).HasMaxLength(40);

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(e => e.SignedAmount);
        builder.HasIndex(e => e.RepresentativeName);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
