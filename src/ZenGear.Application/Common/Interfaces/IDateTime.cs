namespace ZenGear.Application.Common.Interfaces;

/// <summary>
/// Abstraction for DateTime to support testing.
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Get current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
