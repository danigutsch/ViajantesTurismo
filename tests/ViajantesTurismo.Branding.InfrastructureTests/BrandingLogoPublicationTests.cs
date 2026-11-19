using System.Text;
using System.Text.Json;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingLogoPublicationTests
{
    [Fact]
    public async Task An_edit_that_keeps_the_logo_publishes_a_bounded_self_contained_snapshot()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);
        var logo = BrandingLogo.FromEncodedBytes(new byte[BrandingLogoLimits.MaxEncodedBytes]);
        await scenario.CreateStore().ReplaceLogoIfMatch(logo, _ => true, ct);
        var edit = BrandingSettingsTestData.CreateDto(new string('é', BrandingContractConstants.MaxBrandNameLength));
        edit.LogoUri = BrandingLogoPath.For(logo.Sha256);
        var changed = BrandingSettings.Create(edit, BrandingDefaults.AllowedFonts).Value;

        // Act
        await scenario.CreateStore().SaveSettings(changed, ct);
        var payloads = scenario.ReadSnapshot().Outbox.Select(row => row.Payload.ShouldNotBeNull()).ToArray();
        var logoPayloads = payloads.Where(payload => payload.Contains("LogoContent", StringComparison.Ordinal)).ToArray();

        // Assert
        logoPayloads.Length.ShouldBe(2);
        var edited = logoPayloads.ShouldHaveSingleItem(payload => payload.Contains("\"SourceRevision\":3", StringComparison.Ordinal));
        using var document = JsonDocument.Parse(edited);
        document.RootElement.GetProperty("LogoSha256").GetString().ShouldBe(logo.Sha256);
        document.RootElement.GetProperty("LogoContent").GetBytesFromBase64().Length.ShouldBe(BrandingLogoLimits.MaxEncodedBytes);
        var fullBytes = Encoding.UTF8.GetByteCount(edited);
        fullBytes.ShouldBeLessThanOrEqualTo(64 * 1024);
        var withoutLogo = payloads.ShouldHaveSingleItem(payload => !payload.Contains("LogoContent", StringComparison.Ordinal));
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Unchanged logo snapshot {fullBytes} bytes. Initial snapshot without a logo {Encoding.UTF8.GetByteCount(withoutLogo)} bytes.");
    }

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
        var payload = scenario.ReadSnapshot().Outbox.Select(row => row.Payload).Where(text => text is not null && text.Contains("LogoSha256", StringComparison.Ordinal)).ShouldHaveSingleItem()!;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        root.GetProperty("LogoSha256").GetString().ShouldBe(logo.Sha256);
        root.GetProperty("LogoContentType").GetString().ShouldBe(BrandingLogo.EncodedContentType);
        root.GetProperty("LogoContent").GetString().ShouldBe(BrandingLogoWireFormatData.LogoBase64);
    }
}
