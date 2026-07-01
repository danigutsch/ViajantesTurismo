using ViajantesTurismo.Catalog.Domain.PublicTheme;

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
        Assert.Empty(events);
        Assert.Empty(content.GetDomainEvents());
    }

    [Fact]
    public void Public_theme_settings_identity_equality_matches_generated_semantics()
    {
        // Arrange
        var first = PublicThemeSettings.Default();
        var second = PublicThemeSettings.Default();
        var createResult = PublicThemeSettings.Create("#112233", "#AABBCC", "#FFFFFF", "#000000", "Inter", "Verdana");
        Assert.True(createResult.IsSuccess);
        var different = createResult.Value;

        // Act
        CatalogIdentityAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }

    [Fact]
    public void Public_theme_settings_exposes_empty_aggregate_events()
    {
        // Arrange
        var theme = PublicThemeSettings.Default();

        // Act
        var events = theme.GetDomainEvents();
        theme.ClearDomainEvents();

        // Assert
        Assert.Empty(events);
        Assert.Empty(theme.GetDomainEvents());
    }

}
