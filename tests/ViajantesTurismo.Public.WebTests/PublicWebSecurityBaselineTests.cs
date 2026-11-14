using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[TestScope(TestScope.Unit)]
public sealed class PublicWebSecurityBaselineTests
{
    [Fact]
    public void A_rejected_request_is_answered_with_too_many_requests()
    {
        // Arrange
        const int tooManyRequests = StatusCodes.Status429TooManyRequests;

        // Act
        var options = PublicWebSecurityBaselineTestsHelpers.ConfiguredRateLimiterOptions();

        // Assert
        options.RejectionStatusCode.ShouldBe(tooManyRequests);
        options.OnRejected.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("/api/v1/public/catalog/tours", HttpStatusCode.TooManyRequests, false)]
    [InlineData("/api/v1/public/branding", HttpStatusCode.TooManyRequests, true)]
    [InlineData("/api/v1/public/catalog/tours", HttpStatusCode.ServiceUnavailable, true)]
    public async Task Only_a_public_catalog_quota_answer_is_final(string path, HttpStatusCode status, bool retried)
    {
        // Arrange
        var options = new HttpStandardResilienceOptions();
        PublicWebSecurityBaseline.ConfigureCatalogResilience(options);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri("https://catalog.test"), path));
        using var response = new HttpResponseMessage(status) { RequestMessage = request };
        var context = ResilienceContextPool.Shared.Get(TestContext.Current.CancellationToken);
        context.SetRequestMessage(request);

        // Act
        var handled = await options.Retry.ShouldHandle(new RetryPredicateArguments<HttpResponseMessage>(context, Outcome.FromResult(response), 0));
        ResilienceContextPool.Shared.Return(context);

        // Assert
        handled.ShouldBe(retried);
    }
}
