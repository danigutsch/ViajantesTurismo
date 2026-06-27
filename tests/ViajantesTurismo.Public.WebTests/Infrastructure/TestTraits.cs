namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>
/// Shared trait constants for Public Web tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Category value for endpoint-focused tests.
    /// </summary>
    public const string EndpointCategory = TestTraitValues.EndpointCategory;

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
    public const string TestServerHost = TestTraitValues.TestServerHost;
}
