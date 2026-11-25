using Microsoft.Playwright;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Workflows;

[Trait(TestTraitNames.AreaName, TestAreas.Shared)]
[TestScope(TestScope.Unit)]
public sealed class ManagementLoginWorkflowTests
{
    [Theory]
    [InlineData("net::ERR_NETWORK_CHANGED at http://localhost:5000/login")]
    [InlineData("navigating to \"chrome-error://chromewebdata/\", waiting until \"load\"")]
    [InlineData("Navigation to \"http://localhost:5000/\" is interrupted by another navigation to \"http://localhost:8080/realms\"")]
    [InlineData("Page URL expected to be 'http://localhost:5000/'\nBut was: 'http://localhost:8080/realms/test/login-actions'\nCall log:\n  - Expect \"ToHaveURLAsync\" with timeout 5000ms")]
    public void Sign_in_retries_a_transient_playwright_failure(string message)
    {
        // Arrange
        var exception = new PlaywrightException(message);

        // Act
        var retryable = ManagementLoginWorkflow.IsRetryableSignInFailure(exception);

        // Assert
        retryable.ShouldBeTrue();
    }

    [Fact]
    public void Sign_in_retries_a_timeout()
    {
        // Arrange
        var exception = new TimeoutException("Timeout 30000ms exceeded.");

        // Act
        var retryable = ManagementLoginWorkflow.IsRetryableSignInFailure(exception);

        // Assert
        retryable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Target page, context or browser has been closed")]
    [InlineData("Element is not attached to the DOM")]
    [InlineData("strict mode violation: Locator(\"#username\") resolved to 2 elements")]
    public void Sign_in_does_not_retry_other_playwright_failures(string message)
    {
        // Arrange
        var exception = new PlaywrightException(message);

        // Act
        var retryable = ManagementLoginWorkflow.IsRetryableSignInFailure(exception);

        // Assert
        retryable.ShouldBeFalse();
    }

    [Fact]
    public void Sign_in_does_not_retry_a_non_playwright_failure()
    {
        // Arrange
        var exception = new InvalidOperationException("Page URL expected to be 'http://localhost:5000/'");

        // Act
        var retryable = ManagementLoginWorkflow.IsRetryableSignInFailure(exception);

        // Assert
        retryable.ShouldBeFalse();
    }
}
