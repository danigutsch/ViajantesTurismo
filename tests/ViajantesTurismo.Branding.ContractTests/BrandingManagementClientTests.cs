using System.Net;
using PandaBear.Branding;
using PandaBear.HttpClients;
using PandaBear.Testing.Http;
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
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(HttpStatusCode.OK, SettingsJson, tag));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var document = outcome.Value.ShouldNotBeNull();
        document.EntityTag.ShouldBe(tag);
        document.Settings.SourceRevision.ShouldBe(4);
        document.Settings.BrandName.ShouldBe("Viajantes");
    }

    [Theory]
    [InlineData("W/\"weak\"")]
    [InlineData(null)]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Read_reports_a_malformed_body_without_a_strong_entity_tag(string? tag)
    {
        // Arrange
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(HttpStatusCode.OK, SettingsJson, tag));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, HttpOutcomeKind.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, HttpOutcomeKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, HttpOutcomeKind.Forbidden)]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Read_reports_the_documented_failure_status_as_an_outcome(HttpStatusCode status, HttpOutcomeKind kind)
    {
        // Arrange
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(status, "{}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(kind);
        outcome.StatusCode.ShouldBe(status);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_sends_the_entity_tag_as_if_match()
    {
        // Arrange
        var tag = BrandingEntityTags.Format(Guid.CreateVersion7(), 4);
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(HttpStatusCode.OK, SettingsJson));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(new BrandingSettingsDto { BrandName = "Viajantes" }, tag, TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Requests.ShouldHaveSingleItem().Message;
        request.Method.ShouldBe(HttpMethod.Put);
        request.Headers.IfMatch.ShouldHaveSingleItem().Tag.ShouldBe(tag);
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var saved = outcome.Value.ShouldNotBeNull();
        saved.SourceRevision.ShouldBe(4);
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [InlineData(HttpStatusCode.PreconditionRequired)]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_reports_a_precondition_status_without_retrying(HttpStatusCode status)
    {
        // Arrange
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(status, "{}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(
            new BrandingSettingsDto { BrandName = "Viajantes" },
            BrandingEntityTags.Format(Guid.CreateVersion7(), 1),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.UnexpectedStatus);
        outcome.StatusCode.ShouldBe(status);
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_reports_a_malformed_if_match_as_a_bad_request_without_validation_errors()
    {
        // Arrange
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(
            HttpStatusCode.BadRequest,
            """{"status":400,"code":"branding.invalid_if_match"}"""));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(
            new BrandingSettingsDto { BrandName = "Viajantes" },
            BrandingEntityTags.Format(Guid.CreateVersion7(), 1),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        outcome.ValidationErrors.ShouldBeNull();
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_reports_an_empty_success_body()
    {
        // Arrange
        using var handler = new TestHttpMessageHandler(BrandingResponses.Json(HttpStatusCode.OK, string.Empty));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(
            new BrandingSettingsDto { BrandName = "Viajantes" },
            BrandingEntityTags.Format(Guid.CreateVersion7(), 1),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.EmptyBody);
        outcome.Value.ShouldBeNull();
    }
}
