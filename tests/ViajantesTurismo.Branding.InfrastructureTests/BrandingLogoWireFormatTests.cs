using System.Text.Json;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingLogoWireFormatTests
{
    [Fact]
    public void The_outbox_serializer_writes_the_logo_as_one_base64_string()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(BrandingLogoWireFormatData.LogoBytes);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var json = serializer.Serialize(integrationEvent);

        // Assert
        json.ShouldContain($"\"LogoContent\":\"{BrandingLogoWireFormatData.LogoBase64}\"", StringComparison.Ordinal);
    }

    [Fact]
    public void The_outbox_serializer_writes_the_same_json_text_as_the_previous_byte_array_payload()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(BrandingLogoWireFormatData.LogoBytes);
        var previous = BrandingLogoWireFormatData.CreatePrevious(BrandingLogoWireFormatData.LogoBytes);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var json = serializer.Serialize(integrationEvent);
        var previousJson = JsonSerializer.Serialize(previous);

        // Assert
        json.ShouldBe(previousJson);
    }

    [Fact]
    public void The_outbox_serializer_omits_the_logo_fields_exactly_like_the_previous_byte_array_payload()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(null);
        var previous = BrandingLogoWireFormatData.CreatePrevious(null);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var json = serializer.Serialize(integrationEvent);
        var previousJson = JsonSerializer.Serialize(previous);

        // Assert
        json.ShouldBe(previousJson);
        json.ShouldNotContain("LogoContent", StringComparison.Ordinal);
        json.ShouldNotContain("LogoSha256", StringComparison.Ordinal);
    }

    [Fact]
    public void A_payload_written_with_the_previous_byte_array_shape_restores_the_exact_bytes()
    {
        // Arrange
        var previousJson = JsonSerializer.Serialize(BrandingLogoWireFormatData.CreatePrevious(BrandingLogoWireFormatData.LogoBytes));
        var typeInfo = BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;

        // Act
        var restored = JsonSerializer.Deserialize(previousJson, typeInfo).ShouldNotBeNull();

        // Assert
        restored.LogoContent.ShouldNotBeNull().ToArray().ShouldBe(BrandingLogoWireFormatData.LogoBytes);
        restored.LogoSha256.ShouldBe("sha");
    }

    [Fact]
    public void A_payload_written_by_the_serializer_is_readable_with_the_previous_byte_array_shape()
    {
        // Arrange
        var json = new BrandingIntegrationEventSerializer().Serialize(BrandingLogoWireFormatData.CreateEvent(BrandingLogoWireFormatData.LogoBytes));

        // Act
        var restored = JsonSerializer.Deserialize<LegacyByteArrayBrandingPayload>(json).ShouldNotBeNull();

        // Assert
        restored.LogoContent.ShouldBe(BrandingLogoWireFormatData.LogoBytes);
    }

    [Fact]
    public void A_payload_written_before_the_logo_fields_existed_restores_without_a_logo()
    {
        // Arrange
        const string json = """
            {"EventId":"0199f0aa-0000-7000-8000-0000000000aa","OccurredAt":"2026-10-24T12:00:00+00:00","SourceEpoch":"0199f0aa-0000-7000-8000-000000000001","SourceRevision":4,"BrandName":"Viajantes","PrimaryColor":"#0F766E","AccentColor":"#F97316","BackgroundColor":"#FFFBF5","TextColor":"#1F2937","HeadingFontFamily":"Georgia","BodyFontFamily":"system-ui","LogoUri":"/logo.svg"}
            """;
        var typeInfo = BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;

        // Act
        var restored = JsonSerializer.Deserialize(json, typeInfo).ShouldNotBeNull();

        // Assert
        restored.LogoContent.ShouldBeNull();
        restored.LogoSha256.ShouldBeNull();
        restored.LogoContentType.ShouldBeNull();
    }

    [Fact]
    public void The_source_generated_context_round_trips_the_bytes()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(BrandingLogoWireFormatData.LogoBytes);
        var typeInfo = BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;

        // Act
        var json = JsonSerializer.Serialize(integrationEvent, typeInfo);
        var restored = JsonSerializer.Deserialize(json, typeInfo).ShouldNotBeNull();

        // Assert
        restored.LogoContent.ShouldNotBeNull().ToArray().ShouldBe(BrandingLogoWireFormatData.LogoBytes);
    }
}
