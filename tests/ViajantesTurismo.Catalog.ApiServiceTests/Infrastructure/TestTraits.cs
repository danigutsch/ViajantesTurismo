namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Trait values used to categorize Catalog API service tests.</summary>
internal static class TestTraits
{
    /// <summary>Trait value for endpoint tests.</summary>
    public const string EndpointCategory = PandaBear.Testing.TestTraitValues.EndpointCategory;
    /// <summary>Trait value for security tests.</summary>
    public const string SecurityCategory = PandaBear.Testing.TestTraitValues.SecurityCategory;
    /// <summary>Trait value for the catalog area.</summary>
    public const string CatalogArea = "catalog";
    /// <summary>Trait value for the branding area.</summary>
    public const string BrandingArea = "branding";
    /// <summary>Trait value for tests hosted on the test server.</summary>
    public const string TestServerHost = PandaBear.Testing.TestTraitValues.TestServerHost;
}
