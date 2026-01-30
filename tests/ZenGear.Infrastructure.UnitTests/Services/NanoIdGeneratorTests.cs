using FluentAssertions;
using ZenGear.Domain.Constants;
using ZenGear.Infrastructure.Services;

namespace ZenGear.Infrastructure.UnitTests.Services;

/// <summary>
/// Unit tests for NanoIdGenerator.
/// Tests external ID generation, validation, and format.
/// </summary>
public class NanoIdGeneratorTests
{
    private readonly NanoIdGenerator _generator;

    public NanoIdGeneratorTests()
    {
        _generator = new NanoIdGenerator();
    }

    [Fact]
    public void Generate_WithValidPrefix_ShouldReturnCorrectFormat()
    {
        // Arrange
        var prefix = EntityPrefixes.User;

        // Act
        var externalId = _generator.Generate(prefix);

        // Assert
        externalId.Should().NotBeNullOrWhiteSpace();
        externalId.Should().StartWith($"{prefix}_");
        
        var parts = externalId.Split('_');
        parts.Should().HaveCount(2);
        parts[0].Should().Be(prefix);
        parts[1].Should().HaveLength(16); // NanoId length
    }

    [Theory]
    [InlineData(EntityPrefixes.User, "usr")]
    [InlineData(EntityPrefixes.Product, "prod")]
    [InlineData(EntityPrefixes.Order, "ord")]
    [InlineData(EntityPrefixes.ProductVariant, "var")]
    public void Generate_WithDifferentPrefixes_ShouldIncludeCorrectPrefix(string prefix, string expectedPrefix)
    {
        // Act
        var externalId = _generator.Generate(prefix);

        // Assert
        externalId.Should().StartWith($"{expectedPrefix}_");
    }

    [Fact]
    public void Generate_CalledMultipleTimes_ShouldGenerateUniqueIds()
    {
        // Arrange
        var prefix = EntityPrefixes.User;
        var ids = new HashSet<string>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var id = _generator.Generate(prefix);
            ids.Add(id);
        }

        // Assert
        ids.Should().HaveCount(1000, "all generated IDs should be unique");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_WithNullOrWhiteSpacePrefix_ShouldThrowArgumentException(string? prefix)
    {
        // Act & Assert
        var act = () => _generator.Generate(prefix!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ShouldOnlyUseUrlSafeCharacters()
    {
        // Arrange
        var prefix = EntityPrefixes.User;
        var urlSafeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";

        // Act
        for (int i = 0; i < 100; i++)
        {
            var externalId = _generator.Generate(prefix);
            var idPart = externalId[(prefix.Length + 1)..];

            // Assert
            idPart.Should().MatchRegex($"^[{urlSafeAlphabet}]{{16}}$",
                "NanoId should only contain URL-safe characters without ambiguous chars (0/O, 1/l/I)");
        }
    }

    [Fact]
    public void IsValid_WithValidExternalId_ShouldReturnTrue()
    {
        // Arrange
        var prefix = EntityPrefixes.User;
        var externalId = _generator.Generate(prefix);

        // Act
        var isValid = _generator.IsValid(externalId, prefix);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("usr_p3k7N9mRsWxYzHdG", EntityPrefixes.User, true)] // Valid - no ambiguous chars
    [InlineData("prod_nY5WL3K9Xq2mR7hT", EntityPrefixes.Product, true)]
    [InlineData("ord_8pM4vJ2NKs6wE9xY", EntityPrefixes.Order, true)]
    [InlineData("usr_p3k7N9mRsWxYzHdG", EntityPrefixes.Product, false)] // Wrong prefix
    [InlineData("usr_short", EntityPrefixes.User, false)] // Too short
    [InlineData("usr_p3k7N9mRsWxYzHdG123456", EntityPrefixes.User, false)] // Too long
    [InlineData("invalid", EntityPrefixes.User, false)] // No underscore
    [InlineData("usr_", EntityPrefixes.User, false)] // No ID part
    [InlineData("", EntityPrefixes.User, false)] // Empty
    public void IsValid_WithVariousFormats_ShouldReturnExpectedResult(
        string externalId,
        string expectedPrefix,
        bool expectedResult)
    {
        // Act
        var isValid = _generator.IsValid(externalId, expectedPrefix);

        // Assert
        isValid.Should().Be(expectedResult);
    }

    [Fact]
    public void IsValid_WithInvalidCharacters_ShouldReturnFalse()
    {
        // Arrange - Contains ambiguous characters (0, O, 1, l, I)
        var externalIdsWithAmbiguous = new[]
        {
            "usr_p3k7N9mRsWxY0HdG", // Contains 0
            "usr_p3k7N9mRsWxYOHdG", // Contains O
            "usr_p3k7N9mRsWxY1HdG", // Contains 1
            "usr_p3k7N9mRsWxYlHdG", // Contains l (lowercase L)
            "usr_p3k7N9mRsWxYIHdG"  // Contains I
        };

        // Act & Assert
        foreach (var externalId in externalIdsWithAmbiguous)
        {
            var isValid = _generator.IsValid(externalId, EntityPrefixes.User);
            isValid.Should().BeFalse($"'{externalId}' contains ambiguous characters");
        }
    }

    [Theory]
    [InlineData("usr_p3k7N9mRsWxYzHdG", "usr")]
    [InlineData("prod_nY5WL3K9Xq2mR7hT", "prod")]
    [InlineData("ord_8pM4vJ2NKs6wE9xY", "ord")]
    [InlineData("var_Fg5hJ7kL9qW1eR3t", "var")]
    public void GetPrefix_WithValidExternalId_ShouldReturnCorrectPrefix(string externalId, string expectedPrefix)
    {
        // Act
        var prefix = _generator.GetPrefix(externalId);

        // Assert
        prefix.Should().Be(expectedPrefix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")] // No underscore
    public void GetPrefix_WithInvalidExternalId_ShouldReturnNull(string? externalId)
    {
        // Act
        var prefix = _generator.GetPrefix(externalId!);

        // Assert
        prefix.Should().BeNull();
    }

    [Fact]
    public void GetPrefix_WithMultipleUnderscores_ShouldReturnFirstPart()
    {
        // Arrange - Edge case with multiple underscores
        var externalId = "usr_test_extra";

        // Act
        var prefix = _generator.GetPrefix(externalId);

        // Assert
        prefix.Should().Be("usr");
    }
}
