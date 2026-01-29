using Microsoft.EntityFrameworkCore;
using ZenGear.Domain.Common;
using ZenGear.Domain.Repositories;

namespace ZenGear.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository base class for aggregate roots.
/// Provides basic CRUD operations.
/// </summary>
public class Repository<T> : IRepository<T>
    where T : class, IAggregateRoot
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Get entity by internal long Id.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await DbSet.FindAsync([id], ct);
    }

    /// <summary>
    /// Get all entities.
    /// WARNING: Use with caution - prefer paginated queries.
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    /// <summary>
    /// Add new entity to repository.
    /// </summary>
    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
    }

    /// <summary>
    /// Update existing entity.
    /// </summary>
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Remove entity from repository.
    /// </summary>
    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
