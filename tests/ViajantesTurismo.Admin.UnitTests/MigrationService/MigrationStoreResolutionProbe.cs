namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>Records which migration-service stores were resolved from DI.</summary>
internal sealed class MigrationStoreResolutionProbe
{
    /// <summary>Whether the branding store was resolved.</summary>
    public bool BrandingResolved
    {
        get; private set;
    }

    /// <summary>Whether the catalog store was resolved.</summary>
    public bool CatalogResolved
    {
        get; private set;
    }

    /// <summary>Whether the management security store was resolved.</summary>
    public bool ManagementSecurityResolved
    {
        get; private set;
    }

    /// <summary>Marks the branding store as resolved.</summary>
    public void RecordBranding() => BrandingResolved = true;

    /// <summary>Marks the catalog store as resolved.</summary>
    public void RecordCatalog() => CatalogResolved = true;

    /// <summary>Marks the management security store as resolved.</summary>
    public void RecordManagementSecurity() => ManagementSecurityResolved = true;
}
