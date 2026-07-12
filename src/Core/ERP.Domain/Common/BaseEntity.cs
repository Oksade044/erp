namespace ERP.Domain.Common;

/// <summary>
/// Bütün entity-lərin əsas tipi. Guid Id, audit sahələri, soft-delete və
/// optimistic concurrency (RowVersion) təmin edir. Bax: TDD §13.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // Audit — EF Core interceptor tərəfindən doldurulur (TDD §20)
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft delete (TDD §13)
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Optimistic concurrency (TDD §35)
    public byte[] RowVersion { get; set; } = [];
}
