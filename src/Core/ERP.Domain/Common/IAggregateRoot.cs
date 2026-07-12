namespace ERP.Domain.Common;

/// <summary>
/// Aggregate root marker interface. Repository-lər yalnız aggregate root-lar
/// üçün açılır (TDD §14). Bu, domenin sərhədlərini qoruyur.
/// </summary>
public interface IAggregateRoot;
