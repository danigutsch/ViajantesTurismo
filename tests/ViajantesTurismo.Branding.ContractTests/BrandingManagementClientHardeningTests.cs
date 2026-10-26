using System.Net;
using System.Net.Http.Headers;
using PandaBear.Branding;
using PandaBear.HttpClients;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Branding.ContractTests;

public sealed class BrandingManagementClientHardeningTests
{
    private const int ResponseCeilingBytes = 1024 * 1024;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Read_reports_a_body_above_the_response_ceiling_as_malformed(bool knownLength)
    {
        // Arrange
        var body = new string(' ', ResponseCeilingBytes + 1);
        using var handler = new RecordingBrandingHandler(_ =>
        {
            var response = RecordingBrandingHandler.Json(HttpStatusCode.OK, body, "\"tag\"");
            if (!knownLength)
            {
                response.Content = new StreamContent(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body)));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            return response;
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_returns_a_validation_problem_with_its_field_errors()
    {
        // Arrange
        using var handler = new RecordingBrandingHandler(_ =>
        {
            var response = RecordingBrandingHandler.Json(
                HttpStatusCode.BadRequest,
                """{"title":"One or more validation errors occurred.","status":400,"errors":{"PrimaryColor":["Use a safe color."]}}""");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/problem+json");
            return response;
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://branding.test") };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(new BrandingSettingsDto(), "\"tag\"", TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.ValidationProblem);
        var errors = outcome.ValidationErrors.ShouldNotBeNull();
        errors["PrimaryColor"].ShouldBe(["Use a safe color."]);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Write_reports_a_timeout_while_reading_a_stalled_response_body()
    {
        // Arrange
        using var responseBody = new BlockingBrandingReadStream();
        using var handler = new RecordingBrandingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(responseBody) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://branding.test"),
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var outcome = await client.SaveSettings(new BrandingSettingsDto(), "\"tag\"", TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.TimedOut);
        responseBody.ReadStarted.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
    public async Task Read_preserves_caller_cancellation_while_reading_the_response_body()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var responseBody = new BlockingBrandingReadStream(cts.Cancel);
        using var handler = new RecordingBrandingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(responseBody) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://branding.test"),
            Timeout = TimeSpan.FromMinutes(1)
        };
        var client = new BrandingManagementClient(httpClient);

        // Act
        var read = () => client.GetSettings(cts.Token);

        // Assert
        var exception = await read.ShouldThrowAssignableTo<OperationCanceledException>();
        exception.CancellationToken.ShouldBe(cts.Token);
    }
}
