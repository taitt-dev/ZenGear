namespace ZenGear.Domain.Common;

/// <summary>
/// Base entity with audit fields for tracking creation and updates.
/// Extends BaseEntity (has ExternalId).
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    /// <summary>
    /// When the entity was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Internal User ID who created this entity.
    /// Note: This is the internal long ID, not ExternalId.
    /// </summary>
    public long? CreatedBy { get; private set; }

    /// <summary>
    /// When the entity was last updated (UTC).
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Internal User ID who last updated this entity.
    /// Note: This is the internal long ID, not ExternalId.
    /// </summary>
    public long? UpdatedBy { get; private set; }

    /// <summary>
    /// Set creation audit info (called by EF Core interceptor).
    /// </summary>
    public void SetCreatedBy(long userId, DateTimeOffset timestamp)
    {
        CreatedBy = userId;
        CreatedAt = timestamp;
    }

    /// <summary>
    /// Set update audit info (called by EF Core interceptor).
    /// </summary>
    public void SetUpdatedBy(long userId, DateTimeOffset timestamp)
    {
        UpdatedBy = userId;
        UpdatedAt = timestamp;
    }
}
