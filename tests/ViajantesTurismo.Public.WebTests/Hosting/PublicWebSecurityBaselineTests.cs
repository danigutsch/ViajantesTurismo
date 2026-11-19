using Microsoft.AspNetCore.Http;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Hosting;

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
}
