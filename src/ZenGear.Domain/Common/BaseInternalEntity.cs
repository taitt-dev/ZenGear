namespace ZenGear.Domain.Common;

/// <summary>
/// Base entity for internal-only entities (not exposed via API).
/// These entities do NOT have an ExternalId.
/// Examples: RefreshToken, EmailOtp, OrderItem, OrderStatusHistory
/// </summary>
public abstract class BaseInternalEntity
{
    /// <summary>
    /// Internal database identifier (BIGSERIAL in PostgreSQL).
    /// This is the only ID for internal entities.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// EF Core constructor (required).
    /// </summary>
    protected BaseInternalEntity() { }
}
