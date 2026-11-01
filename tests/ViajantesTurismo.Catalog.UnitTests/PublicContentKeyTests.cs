using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Protects canonical public-content identity and its existing validation contract.</summary>
public sealed class PublicContentKeyTests
{
    [Theory]
    [InlineData(" Home.Hero ", "HOME.HERO")]
    [InlineData("home.hero", "HOME.HERO")]
    [InlineData("section-a", "SECTION-A")]
    [InlineData(" home   hero ", "HOME HERO")]
    [InlineData("home.section/élève", "HOME.SECTION/ÉLÈVE")]
    [InlineData("home\u0000.hero", "HOME.HERO")]
    [InlineData("home.e\u0301", "HOME.É")]
    [InlineData("home.🚲", "HOME.🚲")]
    public void Canonical_keys_preserve_existing_sanitization_and_value_identity(string input, string expected)
    {
        // Arrange
        var canonical = PublicContentKey.Create(expected).Value;

        // Act
        var result = PublicContentKey.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
        result.Value.ShouldBe(canonical);
        new HashSet<PublicContentKey> { canonical, result.Value }.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Empty_keys_preserve_the_key_validation_field(string? input)
    {
        // Act
        var result = PublicContentKey.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("key");
    }

    [Theory]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    [InlineData('\uFFFE')]
    public void Unicode_that_cannot_be_normalized_returns_a_key_validation_error(char invalidCharacter)
    {
        // Arrange
        var input = $"home.{invalidCharacter}.hero";

        // Act
        var result = PublicContentKey.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("key");
    }

    [Fact]
    public void Length_is_validated_after_sanitization()
    {
        // Arrange
        var atLimit = new string('a', CatalogDomainLimits.MaxDefaultLength);

        // Act
        var accepted = PublicContentKey.Create($" {atLimit} ");
        var rejected = PublicContentKey.Create(atLimit + "a");

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        rejected.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("home.hero", "HOME.HERO")]
    [InlineData("home.i\u0307", "HOME.I\u0307")]
    public void Factory_values_materialize_without_changing_the_persisted_identity(string input, string expected)
    {
        // Arrange
        var created = PublicContentKey.Create(input).Value;

        // Act
        var materialized = PublicContentKey.RequireCanonical(created.Value);

        // Assert
        created.Value.ShouldBe(expected);
        materialized.Value.ShouldBe(expected);
        materialized.ShouldBe(created);
    }

    [Fact]
    public void Persisted_length_limit_applies_to_the_exact_stored_identity()
    {
        // Arrange
        var value = string.Concat(Enumerable.Repeat("E\u0301", CatalogDomainLimits.MaxDefaultLength));

        // Act
        Action read = () => _ = PublicContentKey.RequireCanonical(value);

        // Assert
        read.ShouldThrow<InvalidOperationException>();
    }

    [Theory]
    [InlineData("home.hero")]
    [InlineData(" HOME.HERO ")]
    [InlineData("")]
    public void Persisted_keys_must_already_be_canonical(string value)
    {
        // Act
        Action read = () => _ = PublicContentKey.RequireCanonical(value);

        // Assert
        read.ShouldThrow<InvalidOperationException>();
    }
}
