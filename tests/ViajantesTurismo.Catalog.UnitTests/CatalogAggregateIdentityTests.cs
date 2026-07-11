namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogAggregateIdentityTests
{
    [Fact]
    public void Editable_public_content_identity_equality_matches_generated_semantics()
    {
        // Arrange
        var first = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "home.hero");
        var second = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "home.footer");
        var different = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "home.about");

        // Act
        CatalogIdentityAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }

    [Fact]
    public void Editable_public_content_exposes_empty_aggregate_events()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var events = content.GetDomainEvents();
        content.ClearDomainEvents();

        // Assert
        (events).ShouldBeEmpty();
        (content.GetDomainEvents()).ShouldBeEmpty();
    }

}
