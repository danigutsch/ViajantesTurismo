using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, Infrastructure.TestTraits.UnitScope)]
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
}
