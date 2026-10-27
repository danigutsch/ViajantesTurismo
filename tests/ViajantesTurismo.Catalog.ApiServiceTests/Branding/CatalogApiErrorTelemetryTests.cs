using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using PandaBear.AspNetCore;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiErrorTelemetryTests
{
    [Fact]
    public async Task Unhandled_exception_returns_problem_json_without_exception_text_and_is_counted()
    {
        // Arrange
        const string secret = "secret-settings-detail";
        var store = TestBrandingSettingsStore.Seeded();
        store.SaveException = new IOException(secret);
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var recorder = new MeterFactoryRecorder(factory.Services.GetRequiredService<IMeterFactory>());

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var contentType = response.Content.Headers.ContentType;
        contentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe("application/problem+json");
        problem.ShouldNotBeNull();
        body.ShouldNotContain(secret, StringComparison.Ordinal);
        body.ShouldNotContain(nameof(IOException), StringComparison.Ordinal);
        recorder.Sum(
            AspNetCoreTelemetry.UnhandledExceptionsCounterName,
            AspNetCoreTelemetry.ErrorTypeTag,
            nameof(IOException)).ShouldBe(1);
    }
}
