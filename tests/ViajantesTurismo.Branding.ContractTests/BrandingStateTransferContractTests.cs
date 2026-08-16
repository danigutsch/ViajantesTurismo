using System.Text.Json;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Branding.ContractTests;

/// <summary>
/// Verifies the public Branding state-transfer event contract.
/// </summary>
public sealed class BrandingStateTransferContractTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.IntegrationEventSurface)]
    public void Branding_state_transfer_contract_round_trips_complete_public_safe_state()
    {
        // Arrange
        const string logoUri = "/branding/logo.svg";
        var eventId = Guid.Parse("0198c854-0735-7906-a801-b1da18b0094d");
        var occurredAt = DateTimeOffset.Parse(
            "2026-08-01T12:30:00Z",
            System.Globalization.CultureInfo.InvariantCulture);
        var sourceEpoch = Guid.Parse("0198c854-3c52-769b-a6b0-17e5f357972a");
        var integrationEvent = new BrandingSettingsChangedIntegrationEvent(
            eventId,
            occurredAt,
            sourceEpoch,
            42,
            "Viajantes Turismo",
            "#123456",
            "#abcdef",
            "#ffffff",
            "#111111",
            "Inter",
            "Source Sans 3",
            new Uri(logoUri, UriKind.RelativeOrAbsolute));
        var withoutLogoEvent = integrationEvent with
        {
            EventId = Guid.Parse("0198c854-0735-7906-a801-b1da18b0094e"),
            LogoUri = null
        };
        string[] expectedProperties =
        [
            "AccentColor",
            "BackgroundColor",
            "BodyFontFamily",
            "BrandName",
            "EventId",
            "HeadingFontFamily",
            "LogoUri",
            "OccurredAt",
            "PrimaryColor",
            "SourceEpoch",
            "SourceRevision",
            "TextColor"
        ];

        // Act
        var json = JsonSerializer.Serialize(
            integrationEvent,
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        var withoutLogoJson = JsonSerializer.Serialize(
            withoutLogoEvent,
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        using var document = JsonDocument.Parse(json);
        using var withoutLogoDocument = JsonDocument.Parse(withoutLogoJson);
        var actualProperties = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var roundTripped = JsonSerializer.Deserialize(
            json,
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        var withoutLogoRoundTripped = JsonSerializer.Deserialize(
            withoutLogoJson,
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);

        // Assert
        BrandingSettingsChangedIntegrationEvent.EventType.ShouldBe("branding.settings.changed");
        BrandingSettingsChangedIntegrationEvent.EventVersion.ShouldBe(1);
        integrationEvent.SourceEpoch.ShouldNotBe(Guid.Empty);
        (integrationEvent.SourceRevision > 0).ShouldBeTrue();
        actualProperties.ShouldBe(expectedProperties);
        roundTripped.ShouldNotBeNull();
        roundTripped.EventId.ShouldBe(eventId);
        roundTripped.OccurredAt.ShouldBe(occurredAt);
        roundTripped.SourceEpoch.ShouldBe(sourceEpoch);
        roundTripped.SourceRevision.ShouldBe(42);
        roundTripped.BrandName.ShouldBe("Viajantes Turismo");
        roundTripped.PrimaryColor.ShouldBe("#123456");
        roundTripped.AccentColor.ShouldBe("#abcdef");
        roundTripped.BackgroundColor.ShouldBe("#ffffff");
        roundTripped.TextColor.ShouldBe("#111111");
        roundTripped.HeadingFontFamily.ShouldBe("Inter");
        roundTripped.BodyFontFamily.ShouldBe("Source Sans 3");
        roundTripped.LogoUri.ShouldBe(new Uri(logoUri, UriKind.RelativeOrAbsolute));
        withoutLogoDocument.RootElement.TryGetProperty("LogoUri", out var withoutLogoProperty).ShouldBeTrue();
        withoutLogoProperty.ValueKind.ShouldBe(JsonValueKind.Null);
        withoutLogoRoundTripped.ShouldNotBeNull();
        withoutLogoRoundTripped.LogoUri.ShouldBeNull();
    }

}
