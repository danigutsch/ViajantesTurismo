using System.Text.Json;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingLogoPublicationTests
{
    [Fact]
    public async Task A_save_without_a_logo_enqueues_an_event_with_no_logo_fields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();

        // Act
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);

        // Assert
        var payload = scenario.ReadSnapshot().Outbox.ShouldHaveSingleItem().Payload.ShouldNotBeNull();
        payload.ShouldNotContain("LogoContent", StringComparison.Ordinal);
        payload.ShouldNotContain("LogoSha256", StringComparison.Ordinal);
        payload.ShouldNotContain("LogoContentType", StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_save_with_a_logo_enqueues_an_event_carrying_all_three_logo_fields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);
        var logo = BrandingLogo.FromEncodedBytes(BrandingLogoWireFormatData.LogoBytes);

        // Act
        await scenario.CreateStore().ReplaceLogoIfMatch(logo, _ => true, ct);

        // Assert
        var payload = scenario.ReadSnapshot().Outbox[^1].Payload.ShouldNotBeNull();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        root.GetProperty("LogoSha256").GetString().ShouldBe(logo.Sha256);
        root.GetProperty("LogoContentType").GetString().ShouldBe(BrandingLogo.EncodedContentType);
        root.GetProperty("LogoContent").GetString().ShouldBe(BrandingLogoWireFormatData.LogoBase64);
    }
}
