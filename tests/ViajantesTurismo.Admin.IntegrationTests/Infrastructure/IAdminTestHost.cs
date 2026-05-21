namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Admin integration/E2E test host seam for API access, URL resolution, and lifecycle control.
/// </summary>
/// <summary>
/// Abstraction for portable admin test host operations.
/// </summary>
/// <summary>
/// Abstraction for portable admin test host operations.
/// </summary>
public interface IAdminTestHost
{
    /// <summary>Test application base URI.</summary>
    Uri Uri { get; }
    /// <summary>Seeds test data for the integration test host.</summary>
    Task Seed();
    /// <summary>Resets the test application to pre-seed state.</summary>
    Task Reset();
    /// <summary>Disposes any transient resources for test lifecycle management.</summary>
    ValueTask DisposeAsync();
}
