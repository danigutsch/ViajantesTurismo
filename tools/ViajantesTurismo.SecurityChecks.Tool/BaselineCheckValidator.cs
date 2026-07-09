namespace ViajantesTurismo.SecurityChecks.Tool;

internal static class BaselineCheckValidator
{
    private static readonly string[] RequiredFiles =
    [
        "docs/security/threat-model.md",
        "docs/security/security-baseline.md",
        "src/ViajantesTurismo.Public.Web/PublicWebSecurityHeaders.cs",
        "src/ViajantesTurismo.Management.Web/ManagementWebSecurityHeaders.cs",
        "src/ViajantesTurismo.Catalog.ApiService/CatalogSecurityBaseline.cs",
        "src/ViajantesTurismo.Admin.ApiService/AdminSecurityBaseline.cs",
        "src/ViajantesTurismo.Admin.ApiService/Customers/CustomerImportEndpoints.cs"
    ];

    private static readonly string[] ThreatModelMarkers =
    [
        "STRIDE-A",
        "Public Web",
        "Management Web",
        "Admin API",
        "Catalog API",
        "Integration Event Worker",
        "Customer PII",
        "Upload and import",
        "#815",
        "#816",
        "#817",
        "#818",
        "#819"
    ];

    public static void Validate(string repositoryRoot)
    {
        foreach (var requiredFile in RequiredFiles)
        {
            _ = ReadRequired(repositoryRoot, requiredFile);
        }

        RequireContains(repositoryRoot, "docs/security/threat-model.md", ThreatModelMarkers);
        RequireContains(repositoryRoot, "docs/security/security-baseline.md", ["Content-Security-Policy", "Rate limiting", "CORS", "Sensitive data logging", "Customer import"]);
        RequireContains(repositoryRoot, "src/ViajantesTurismo.Catalog.ApiService/Program.cs", ["AddCatalogSecurityBaseline", "UseCors", "UseRateLimiter"]);
        RequireContains(repositoryRoot, "src/ViajantesTurismo.Admin.ApiService/Program.cs", ["AddAdminSecurityBaseline", "UseCors", "UseRateLimiter"]);
        RequireContains(repositoryRoot, "src/ViajantesTurismo.Public.Web/Program.cs", ["UsePublicWebSecurityHeaders"]);
        RequireContains(repositoryRoot, "src/ViajantesTurismo.Management.Web/Program.cs", ["UseManagementWebSecurityHeaders"]);

        var catalogInfrastructure = ReadRequired(repositoryRoot, "src/ViajantesTurismo.Catalog.Infrastructure/InfrastructureDependencyInjection.cs");
        if (catalogInfrastructure.Contains("EnableSensitiveDataLogging", StringComparison.Ordinal) && !catalogInfrastructure.Contains("IsDevelopment", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Catalog sensitive data logging must stay development-gated.");
        }
    }

    private static void RequireContains(string repositoryRoot, string relativePath, IReadOnlyCollection<string> markers)
    {
        var content = ReadRequired(repositoryRoot, relativePath);
        var missingMarker = markers.FirstOrDefault(marker => !content.Contains(marker, StringComparison.Ordinal));
        if (missingMarker is not null)
        {
            throw new InvalidOperationException($"{relativePath} is missing required marker: {missingMarker}");
        }
    }

    private static string ReadRequired(string repositoryRoot, string relativePath)
    {
        var path = Path.Combine(repositoryRoot, relativePath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Missing required security baseline file: {relativePath}");
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Could not read required security baseline file: {relativePath}", ex);
        }
    }
}
