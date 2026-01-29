using NanoidDotNet;
using ZenGear.Domain.Common;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// NanoId-based external ID generator.
/// Format: {prefix}_{16-char-nanoid}
/// Example: usr_V1StGXR8Z5jdHi6B
/// Uses the Nanoid package: https://github.com/codeyu/nanoid-net
/// </summary>
public class NanoIdGenerator : IExternalIdGenerator
{
    // URL-safe alphabet, no ambiguous characters (0/O, 1/l/I)
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
    private const int IdLength = 16;

    /// <summary>
    /// Generate external ID with entity prefix.
    /// </summary>
    public string Generate(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        var id = Nanoid.Generate(Alphabet, IdLength);
        return $"{prefix}_{id}";
    }

    /// <summary>
    /// Validate external ID format and prefix.
    /// </summary>
    public bool IsValid(string externalId, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return false;

        if (!externalId.StartsWith($"{expectedPrefix}_"))
            return false;

        var idPart = externalId[(expectedPrefix.Length + 1)..];
        return idPart.Length == IdLength && idPart.All(c => Alphabet.Contains(c));
    }

    /// <summary>
    /// Extract prefix from external ID.
    /// </summary>
    public string? GetPrefix(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        var underscoreIndex = externalId.IndexOf('_');
        return underscoreIndex > 0 ? externalId[..underscoreIndex] : null;
    }
}
