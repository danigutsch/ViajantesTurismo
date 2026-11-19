using System.Text;
using System.Text.Json;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingLogoWireFormatTests
{
    [Fact]
    public void A_full_snapshot_with_a_maximum_size_logo_fits_the_message_budget()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(new byte[BrandingLogoLimits.MaxEncodedBytes]);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var payload = serializer.Serialize(integrationEvent);
        var bytes = Encoding.UTF8.GetByteCount(payload);

        // Assert
        BrandingLogoLimits.MaxEncodedBytes.ShouldBe(BrandingEventLimits.MaxLogoBytes);
        bytes.ShouldBeLessThanOrEqualTo(64 * 1024);
    }

    [Fact]
    public void The_outbox_refuses_a_serialized_event_over_the_message_budget()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(null) with
        {
            BrandName = new string('x', 64 * 1024)
        };
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var serialize = () => serializer.Serialize(integrationEvent);

        // Assert
        serialize.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void The_outbox_refuses_logo_bytes_over_the_logo_budget_even_if_the_event_fits()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(new byte[BrandingLogoLimits.MaxEncodedBytes + 1]);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var serialize = () => serializer.Serialize(integrationEvent);

        // Assert
        serialize.ShouldThrow<InvalidOperationException>();
    }

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
    public void The_outbox_serializer_omits_the_logo_fields_when_there_is_no_logo()
    {
        // Arrange
        var integrationEvent = BrandingLogoWireFormatData.CreateEvent(null);
        var serializer = new BrandingIntegrationEventSerializer();

        // Act
        var json = serializer.Serialize(integrationEvent);

        // Assert
        json.ShouldNotContain("LogoContent", StringComparison.Ordinal);
        json.ShouldNotContain("LogoSha256", StringComparison.Ordinal);
    }

    [Fact]
    public void A_payload_without_logo_fields_restores_without_a_logo()
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
