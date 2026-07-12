namespace ViajantesTurismo.Admin.SystemTests.Infrastructure;

/// <summary>
/// Shared trait constants for Admin system tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Category value for migration-focused tests.
    /// </summary>
    public const string MigrationCategory = "migration";

    /// <summary>
    /// Scope value for system tests.
    /// </summary>
    public const string SystemScope = "system";

    /// <summary>
    /// Area value for shared system test coverage.
    /// </summary>
    public const string SharedArea = "shared";

    /// <summary>
    /// Host value for Aspire-hosted tests.
    /// </summary>
    public const string AspireHost = "aspire";

    /// <summary>
    /// Area value for post-transport Catalog and Public Web validation.
    /// </summary>
    public const string PostTransportArea = "post-transport";

    /// <summary>
    /// Category value for integration-event transport validation.
    /// </summary>
    public const string IntegrationEventTransportCategory = "integration-event-transport";

    /// <summary>
    /// Category value for authentication conformance tests.
    /// </summary>
    public const string AuthenticationCategory = "authentication";

    /// <summary>
    /// Surface value for Admin validation.
    /// </summary>
    public const string AdminSurface = "admin";

    /// <summary>
    /// Surface value for Catalog validation.
    /// </summary>
    public const string CatalogSurface = "catalog";

    /// <summary>
    /// Surface value for Public Web validation.
    /// </summary>
    public const string PublicWebSurface = "public-web";
}
