namespace ZenGear.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots.
/// Only aggregate roots can be retrieved from repositories.
/// Aggregate roots are the entry point to a consistency boundary.
/// </summary>
public interface IAggregateRoot
{
}
