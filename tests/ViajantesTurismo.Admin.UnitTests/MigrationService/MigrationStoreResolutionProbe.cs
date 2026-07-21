namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class MigrationStoreResolutionProbe
{
    public bool BrandingResolved { get; private set; }

    public bool CatalogResolved { get; private set; }

    public bool ManagementSecurityResolved { get; private set; }

    public void RecordBranding() => BrandingResolved = true;

    public void RecordCatalog() => CatalogResolved = true;

    public void RecordManagementSecurity() => ManagementSecurityResolved = true;
}
