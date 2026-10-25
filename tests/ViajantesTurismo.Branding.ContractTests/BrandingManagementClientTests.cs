using System.Net;
using PandaBear.Branding;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Branding.ContractTests;

public sealed class BrandingManagementClientTests
{
    private const string SettingsJson =
        """{"brandName":"Viajantes","primaryColor":"#112233","accentColor":"#445566","backgroundColor":"#FFFFFF","textColor":"#000000","headingFontFamily":"Inter","bodyFontFamily":"Verdana","logoUri":null,"sourceEpoch":"0192f3a4-0000-7000-8000-000000000001","sourceRevision":4}""";

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Read_returns_the_document_with_its_strong_entity_tag()
    {
        // Arrange
        var tag = BrandingEntityTags.Format(Guid.Parse("0192f3a4-0000-7000-8000-000000000001"), 4);
        using var handler = new RecordingBrandingHandler(_ => RecordingBrandingHandler.Json(HttpStatusCode.OK, SettingsJson, tag));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var document = await client.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        document.EntityTag.ShouldBe(tag);
        document.Settings.SourceRevision.ShouldBe(4);
        document.Settings.BrandName.ShouldBe("Viajantes");
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_sends_the_entity_tag_as_if_match()
    {
        // Arrange
        var tag = BrandingEntityTags.Format(Guid.CreateVersion7(), 4);
        using var handler = new RecordingBrandingHandler(_ => RecordingBrandingHandler.Json(HttpStatusCode.OK, SettingsJson));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var saved = await client.SaveSettings(new BrandingSettingsDto { BrandName = "Viajantes" }, tag, TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Put);
        request.Headers.IfMatch.ShouldHaveSingleItem().Tag.ShouldBe(tag);
        saved.SourceRevision.ShouldBe(4);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_reports_a_lost_update_without_retrying()
    {
        // Arrange
        using var handler = new RecordingBrandingHandler(_ => RecordingBrandingHandler.Json(HttpStatusCode.PreconditionFailed, "{}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var save = () => client.SaveSettings(
            new BrandingSettingsDto { BrandName = "Viajantes" },
            BrandingEntityTags.Format(Guid.CreateVersion7(), 1),
            TestContext.Current.CancellationToken);

        // Assert
        await save.ShouldThrow<BrandingPreconditionFailedException>();
        handler.Requests.Count.ShouldBe(1);
    }
}
