using ZenGear.Domain.Common;

namespace ZenGear.Domain.Repositories;

/// <summary>
/// Base repository interface for aggregate roots.
/// Generic CRUD operations using internal long Id.
/// </summary>
/// <typeparam name="T">Aggregate root type</typeparam>
public interface IRepository<T> where T : class, IAggregateRoot
{
    /// <summary>
    /// Get entity by internal Id.
    /// </summary>
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Get all entities.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Add new entity.
    /// </summary>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Update existing entity.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Remove entity.
    /// </summary>
    void Remove(T entity);
}
