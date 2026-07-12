using ERP.Domain.Common;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Ümumi repository — yalnız aggregate root-lar üçün (TDD §14).
/// İnterfeys Application-da, implementasiya Infrastructure-da → Application SQL bilmir.
/// </summary>
public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
