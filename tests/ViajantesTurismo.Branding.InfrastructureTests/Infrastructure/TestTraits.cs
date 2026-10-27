namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Trait values used to categorize Branding infrastructure tests.</summary>
internal static class TestTraits
{
    /// <summary>Trait value for endpoint tests.</summary>
    public const string EndpointCategory = PandaBear.Testing.TestTraitValues.EndpointCategory;
    /// <summary>Trait value for security tests.</summary>
    public const string SecurityCategory = PandaBear.Testing.TestTraitValues.SecurityCategory;
    /// <summary>Trait value for API integration tests.</summary>
    public const string ApiIntegrationScope = PandaBear.Testing.TestTraitValues.ApiIntegrationScope;
    /// <summary>Trait value for the Branding area.</summary>
    public const string BrandingArea = "branding";
    /// <summary>Trait value for tests that use a test server host.</summary>
    public const string TestServerHost = PandaBear.Testing.TestTraitValues.TestServerHost;
}
