namespace ZenGear.Domain.Common;

/// <summary>
/// Generates external IDs for entities exposed via API.
/// Implementation is in Infrastructure layer to avoid domain dependency on external libraries.
/// </summary>
public interface IExternalIdGenerator
{
    /// <summary>
    /// Generate external ID with entity prefix.
    /// Example: Generate("usr") => "usr_V1StGXR8Z5jdHi6B"
    /// </summary>
    /// <param name="prefix">Entity prefix (e.g., "usr", "prod")</param>
    /// <returns>Generated external ID in format: {prefix}_{16-char-nanoid}</returns>
    string Generate(string prefix);

    /// <summary>
    /// Validate external ID format and prefix.
    /// </summary>
    /// <param name="externalId">External ID to validate</param>
    /// <param name="expectedPrefix">Expected prefix</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValid(string externalId, string expectedPrefix);

    /// <summary>
    /// Extract prefix from external ID.
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <returns>Prefix or null if invalid</returns>
    string? GetPrefix(string externalId);
}
