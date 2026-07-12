namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Bir biznes əməliyyatındakı bütün dəyişiklikləri tək transaction-da commit edir
/// (TDD §15). Ya hamısı, ya heç biri.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
