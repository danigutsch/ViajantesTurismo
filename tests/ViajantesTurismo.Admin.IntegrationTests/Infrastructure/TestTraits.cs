namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Shared trait constants for Admin integration tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Category value for smoke coverage.
    /// </summary>
    public const string SmokeCategory = "smoke";

    /// <summary>
    /// Category value for endpoint coverage.
    /// </summary>
    public const string EndpointCategory = SharedKernel.Testing.TestTraitValues.EndpointCategory;

    /// <summary>
    /// Scope value for integration tests.
    /// </summary>
    public const string IntegrationScope = "integration";

    /// <summary>
    /// Host value for Aspire-backed tests.
    /// </summary>
    public const string AspireHost = "aspire";

    /// <summary>
    /// Area value for bookings tests.
    /// </summary>
    public const string BookingsArea = "bookings";

    /// <summary>
    /// Scope value for database integration tests.
    /// </summary>
    public const string DatabaseIntegrationScope = "database-integration";

    /// <summary>
    /// Area value for Branding tests.
    /// </summary>
    public const string BrandingArea = "branding";

    /// <summary>
    /// Trait name for test component ownership.
    /// </summary>
    public const string ComponentName = "Component";

    /// <summary>
    /// Component value for PostgreSQL observability tests.
    /// </summary>
    public const string PostgreSqlObservabilityComponent = "SharedKernel.Observability.Npgsql";
}
