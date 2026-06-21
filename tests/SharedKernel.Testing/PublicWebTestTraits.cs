namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>
/// Shared trait constants for Public Web tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for category.
    /// </summary>
    public const string CategoryName = SharedKernel.Testing.AdminTestTraitNames.CategoryName;

    /// <summary>
    /// Trait name for scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.AdminTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for area.
    /// </summary>
    public const string AreaName = SharedKernel.Testing.AdminTestTraitNames.AreaName;

    /// <summary>
    /// Trait name for host.
    /// </summary>
    public const string HostName = SharedKernel.Testing.AdminTestTraitNames.HostName;

    /// <summary>
    /// Category value for endpoint-focused tests.
    /// </summary>
    public const string EndpointCategory = "endpoint";

    /// <summary>
    /// Scope value for web integration tests.
    /// </summary>
    public const string WebIntegrationScope = "web-integration";

    /// <summary>
    /// Area value for public web test coverage.
    /// </summary>
    public const string PublicWebArea = "public-web";

    /// <summary>
    /// Host value for TestServer-hosted tests.
    /// </summary>
    public const string TestServerHost = "test-server";
}
