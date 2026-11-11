using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
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
        var services = new ServiceCollection().AddPublicWebSecurityBaseline();
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        // Assert
        options.RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
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
