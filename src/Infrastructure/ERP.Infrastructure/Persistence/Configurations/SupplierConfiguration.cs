using ERP.Domain.Modules.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Supplier EF Core konfiqurasiyası. Value object-lər owned type; provider-agnostik (TDD §4, §14).
/// </summary>
public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.TaxId).HasMaxLength(20);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.IsActive).IsRequired();

        // V2 (#15) əlavə sahələr.
        builder.Property(s => s.CompanyName).HasMaxLength(200);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.WhatsApp).HasMaxLength(40);
        builder.Property(s => s.WeChat).HasMaxLength(60);
        builder.Property(s => s.Position).HasMaxLength(100);

        builder.OwnsOne(s => s.Phone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("Phone").HasMaxLength(20).IsRequired();
            phone.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(s => s.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256);
        });

        builder.OwnsOne(s => s.Address, addr =>
        {
            addr.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            addr.Property(a => a.Line).HasColumnName("AddressLine").HasMaxLength(500);
        });

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
