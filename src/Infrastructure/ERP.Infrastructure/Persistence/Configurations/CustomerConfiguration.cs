using ERP.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Customer EF Core konfiqurasiyası. Value object-lər owned type kimi map olunur;
/// provider-a xas SQL yoxdur (TDD §4, §14).
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.TaxId).HasMaxLength(20);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.IsActive).IsRequired();

        // Telefon — owned VO, məcburi. Unikal indekslə ikiqat qeydin qarşısı alınır.
        builder.OwnsOne(c => c.Phone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("Phone").HasMaxLength(20).IsRequired();
            phone.HasIndex(p => p.Value).IsUnique();
        });

        // E-poçt — owned VO, opsional.
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256);
        });

        // Ünvan — owned VO, opsional.
        builder.OwnsOne(c => c.Address, addr =>
        {
            addr.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            addr.Property(a => a.Line).HasColumnName("AddressLine").HasMaxLength(500);
        });

        // Soft delete — silinmişlər avtomatik sorğulardan çıxarılır (TDD §13).
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Qeyd: RowVersion (optimistic concurrency) provider-aware konfiqurasiyası
        // AppDbContext.OnModelCreating-də mərkəzləşdirilib (bütün BaseEntity-lər üçün).
    }
}
