using Microsoft.EntityFrameworkCore.Storage;
using ZenGear.Domain.Repositories;

namespace ZenGear.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unit of Work implementation for transaction management.
/// Coordinates changes across multiple repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Save all changes to the database.
    /// Domain events are dispatched automatically in DbContext.SaveChangesAsync.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Begin a database transaction.
    /// Use for operations that need explicit transaction control.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// Commit the current transaction.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction has not been started.");

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rollback the current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction has not been started.");

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
