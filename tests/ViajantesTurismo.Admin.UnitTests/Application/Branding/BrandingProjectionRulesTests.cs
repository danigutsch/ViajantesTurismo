using System.Security.Cryptography;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
public sealed class BrandingProjectionRulesTests
{
    [Fact]
    public void The_first_revision_is_applied_to_an_empty_projection()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.Apply);
    }

    [Fact]
    public void A_newer_revision_is_applied_even_after_a_gap()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEvent(revision: 2));
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 5, brandName: "Newer");

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.Apply);
    }

    [Fact]
    public void An_older_revision_is_stale()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEvent(revision: 3));
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 2);

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.Stale);
    }

    [Fact]
    public void The_same_revision_with_the_same_values_is_already_applied()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEvent(revision: 3));
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 3);

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.AlreadyApplied);
    }

    [Fact]
    public void The_same_revision_with_different_values_is_a_conflict()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEvent(revision: 3));
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Different");

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.RevisionConflict);
    }

    [Fact]
    public void An_unexpected_epoch_fails_closed()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEvent(revision: 3));
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 9, epoch: Guid.CreateVersion7());

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.EpochMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_revision_is_an_invalid_envelope(long revision)
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(revision: revision);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidEnvelope);
    }

    [Fact]
    public void An_empty_epoch_is_an_invalid_envelope()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1, epoch: Guid.Empty);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidEnvelope);
    }

    [Fact]
    public void An_event_without_logo_fields_is_valid()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1);

        // Act
        var valid = BrandingProjectionRules.HasValidLogo(incoming);

        // Assert
        valid.ShouldBeTrue();
    }

    [Fact]
    public void A_complete_matching_webp_logo_is_valid()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEventWithLogo(1, DocumentLogoTestData.Content);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.Apply);
    }

    [Fact]
    public void A_logo_whose_hash_does_not_match_its_bytes_is_invalid()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(1, logoSha256: new string('0', 64), logoContentType: "image/webp", logoContent: DocumentLogoTestData.Content);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidLogo);
    }

    [Fact]
    public void A_logo_over_forty_kibibytes_is_invalid()
    {
        // Arrange
        var content = new byte[(40 * 1024) + 1];
        var incoming = BrandingProjectionTestData.CreateEvent(
            1,
            logoSha256: Convert.ToHexStringLower(SHA256.HashData(content)),
            logoContentType: "image/webp",
            logoContent: content);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidLogo);
    }

    [Fact]
    public void A_logo_that_is_not_webp_is_invalid()
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(
            1,
            logoSha256: DocumentLogoTestData.Sha256,
            logoContentType: "image/png",
            logoContent: DocumentLogoTestData.Content);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidLogo);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void A_partial_logo_is_invalid(bool withHash, bool withType, bool withContent)
    {
        // Arrange
        var incoming = BrandingProjectionTestData.CreateEvent(
            1,
            logoSha256: withHash ? DocumentLogoTestData.Sha256 : null,
            logoContentType: withType ? "image/webp" : null,
            logoContent: withContent ? DocumentLogoTestData.Content : null);

        // Act
        var decision = BrandingProjectionRules.Decide(null, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.InvalidLogo);
    }

    [Fact]
    public void A_replayed_revision_with_the_same_logo_hash_is_already_applied()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEventWithLogo(3, DocumentLogoTestData.Content));
        var incoming = BrandingProjectionTestData.CreateEventWithLogo(3, DocumentLogoTestData.Content);

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.AlreadyApplied);
    }

    [Fact]
    public void The_same_revision_with_a_different_logo_is_a_conflict()
    {
        // Arrange
        var current = BrandingProjectionState.From(BrandingProjectionTestData.CreateEventWithLogo(3, DocumentLogoTestData.Content));
        var incoming = BrandingProjectionTestData.CreateEventWithLogo(3, DocumentLogoTestData.OtherContent);

        // Act
        var decision = BrandingProjectionRules.Decide(current, incoming);

        // Assert
        decision.ShouldBe(BrandingProjectionDecision.RevisionConflict);
    }
}
