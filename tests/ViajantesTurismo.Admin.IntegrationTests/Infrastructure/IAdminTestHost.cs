namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Admin integration/E2E test host seam for API access, URL resolution, and lifecycle control.
/// </summary>
public interface IAdminTestHost
{
    /// <summary>
    /// Gets a configured API HttpClient instance for integration/E2E tests.
    /// </summary>
    HttpClient GetApiClient();
    /// <summary>
    /// Gets the base URL of the Admin API for browser or HTTP tests.
    /// </summary>
    string GetBaseUrl();
    /// <summary>
    /// Performs database seeding for test setup.
    /// </summary>
    Task Seed(CancellationToken ct);
    /// <summary>
    /// Performs database cleanup or reset for test teardown.
    /// </summary>
    Task Reset(CancellationToken ct);
}