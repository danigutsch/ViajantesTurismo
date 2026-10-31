using System.Text.Json;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
public sealed class BrandingEventJsonRoundTripTests
{
    [Fact]
    public void An_event_without_a_logo_round_trips_with_null_logo_fields()
    {
        // Arrange
        var original = BrandingProjectionTestData.CreateEvent(revision: 4);
        var typeInfo = BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;

        // Act
        var json = JsonSerializer.Serialize(original, typeInfo);
        var restored = JsonSerializer.Deserialize(json, typeInfo).ShouldNotBeNull();

        // Assert
        json.ShouldNotContain("LogoContent", StringComparison.Ordinal);
        restored.LogoSha256.ShouldBeNull();
        restored.LogoContentType.ShouldBeNull();
        restored.LogoContent.ShouldBeNull();
    }

    [Fact]
    public void An_event_with_a_logo_round_trips_the_exact_bytes()
    {
        // Arrange
        var original = BrandingProjectionTestData.CreateEventWithLogo(4, DocumentLogoTestData.Content);
        var typeInfo = BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;

        // Act
        var json = JsonSerializer.Serialize(original, typeInfo);
        var restored = JsonSerializer.Deserialize(json, typeInfo).ShouldNotBeNull();

        // Assert
        restored.LogoSha256.ShouldBe(DocumentLogoTestData.Sha256);
        restored.LogoContentType.ShouldBe("image/webp");
        restored.LogoContent.ShouldBe(DocumentLogoTestData.Content);
    }
}
