namespace SharedKernel.Testing;

/// <summary>
/// Shared trait constants for Public Web tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for category.
    /// </summary>
    public const string CategoryName = AdminTestTraitNames.CategoryName;

    /// <summary>
    /// Trait name for scope.
    /// </summary>
    public const string ScopeName = AdminTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for area.
    /// </summary>
    public const string AreaName = AdminTestTraitNames.AreaName;

    /// <summary>
    /// Trait name for host.
    /// </summary>
    public const string HostName = AdminTestTraitNames.HostName;

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
