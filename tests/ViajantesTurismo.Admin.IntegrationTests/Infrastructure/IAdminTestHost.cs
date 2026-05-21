namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Abstraction for admin integration test host operations for Admin integration tests only.
/// </summary>

/// <summary>
/// Abstraction for portable admin test host operations.
/// </summary>
/// <summary>
/// Abstraction for portable admin test host operations.
/// </summary>
public interface IAdminTestHost
{
    /// <summary>
    /// Test application base URI, seed/clear/teardown hooks for integration test host lifecycle.
    /// </summary>
    Uri Uri { get; }
    /// <summary>
    /// Seed test data and reset infra for admin integration tests.
    /// </summary>
    Task Seed();
    Task Reset();
    ValueTask DisposeAsync();
}
